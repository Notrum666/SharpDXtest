using System;
using System.Collections.Generic;
using System.Reflection;

using Engine.BaseAssets.Components.Colliders;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public abstract class Collider : BehaviourComponent
    {
        private const int EpaMaxIter = 4096;
        private static readonly PolygonDistanceComparer distanceComparer = new PolygonDistanceComparer();

        [SerializedField]
        protected double massPart = 1.0;
        [SerializedField]
        protected Vector3 offset;
        [SerializedField]
        protected bool isTrigger = false;
        public bool IsTrigger
        {
            get => isTrigger;
            set => isTrigger = value;
        }

        public delegate void ColliderEvent(Collider sender, Collider other);
        /// <summary>
        /// Called when collision begins (when there is collision on current frame, but no collision on previous frame)
        /// </summary>
        public event ColliderEvent OnCollisionBegin;
        /// <summary>
        /// Called while collision stays (when there is collision on current frame and collision on previous frame)
        /// </summary>
        public event ColliderEvent OnCollision;
        /// <summary>
        /// Called after collision ends (when there is no collision on current frame, but there is collision on previous frame)
        /// </summary>
        public event ColliderEvent OnCollisionEnd;

        /// <summary>
        /// Called when other collider enters the trigger (when there is intersection on current frame, but no intersection on previous frame)
        /// </summary>
        public event ColliderEvent OnTriggerEnter;
        /// <summary>
        /// Called while other collider stays in the trigger (when there is intersection on current frame and intersection on previous frame)
        /// </summary>
        public event ColliderEvent OnTriggerStay;
        /// <summary>
        /// Called after other collider leaves the trigger (when there is no intersection on current frame, but there is an intersection on previous frame)
        /// </summary>
        public event ColliderEvent OnTriggerExit;

        private HashSet<Collider> prevCollidingColliders = new HashSet<Collider>();
        private HashSet<Collider> collidingColliders = new HashSet<Collider>();

        public Ranged<double> MassPart => new Ranged<double>(ref massPart, min: 0);
        public Vector3 Offset
        {
            get => offset;
            set
            {
                offset = value;
                UpdateData();
            }
        }

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);

            switch (fieldInfo.Name)
            {
                case nameof(massPart):
                    MassPart.Set(massPart);
                    return;
            }
            switch (fieldInfo.Name)
            {
                case nameof(offset):
                    Offset = offset;
                    return;
            }
        }
        
        internal override void OnDeserialized()
        {
            UpdateData();
        }

        public Vector3 GlobalCenter { get; private set; }

        public abstract Vector3 InertiaTensor { get; }
        public abstract double OuterSphereRadius { get; }
        public abstract double SquaredOuterSphereRadius { get; }

        protected abstract List<Vector3> GetVertexesOnPlane(Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon);
        protected abstract void GetBoundaryPointsInDirection(Vector3 direction, out Vector3 hindmost, out Vector3 furthest);

        internal virtual void UpdateData()
        {
            GlobalCenter = GameObject.Transform.Model.TransformPoint(Offset);
        }

        internal void UpdateCollidingColliders()
        {
            if (IsTrigger)
            {
                foreach (Collider col in prevCollidingColliders)
                {
                    if (!collidingColliders.Contains(col))
                        OnTriggerExit?.Invoke(this, col);
                }
                foreach (Collider col in collidingColliders)
                {
                    if (!prevCollidingColliders.Contains(col))
                        OnTriggerEnter?.Invoke(this, col);
                    OnTriggerStay?.Invoke(this, col);
                }
            }
            else
            {
                foreach (Collider col in prevCollidingColliders)
                {
                    if (!collidingColliders.Contains(col))
                        OnCollisionEnd?.Invoke(this, col);
                }
                foreach (Collider col in collidingColliders)
                {
                    if (!prevCollidingColliders.Contains(col))
                        OnCollisionBegin?.Invoke(this, col);
                    OnCollision?.Invoke(this, col);
                }
            }

            prevCollidingColliders = new HashSet<Collider>(collidingColliders);
            collidingColliders.Clear();
        }

        private bool GetCollisionExitVector(Collider other, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            bool result = GetCollisionExitVector_SAT(other, out collisionExitVector, out exitDirectionVector, out colliderEndPoint);
            //bool result = GetCollisionExitVector_GJK_EPA(other, out collisionExitVector, out exitDirectionVector, out colliderEndPoint);
            if (result)
            {
                collidingColliders.Add(other);
                other.collidingColliders.Add(this);
            }
            return result;
        }

        internal void ResolveInteractionWith(Collider other, Rigidbody rigidbody)
        {
            if (!LocalEnabled || !other.LocalEnabled)
                return;

            Rigidbody otherRigidbody = other.GameObject.GetComponent<Rigidbody>();

            if ((rigidbody is null || !rigidbody.LocalEnabled) && (otherRigidbody is null || !otherRigidbody.LocalEnabled))
                return;

            if (!GetCollisionExitVector(other, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint))
                return;

            if (rigidbody is null || !rigidbody.LocalEnabled || otherRigidbody is null || !otherRigidbody.LocalEnabled ||
                rigidbody.IsStatic && otherRigidbody.IsStatic)
                return;

            rigidbody.ReactToCollision(otherRigidbody, this, other, (Vector3)collisionExitVector!, (Vector3)exitDirectionVector!, (Vector3)colliderEndPoint!);
        }

        internal static Vector3 GetAverageCollisionPoint(Collider collider1, Collider collider2, Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal)
        {
            Vector3 point = GetAverageCollisionPointWithEpsilon(collider1, collider2, collisionPlanePoint, collisionPlaneNormal, Constants.FloatEpsilon);
            return point;
            //return double.IsNaN(point.x) || double.IsNaN(point.y) || double.IsNaN(point.z) ?
            //    GetAverageCollisionPointWithEpsilon(collider1, collider2, collisionPlanePoint, collisionPlaneNormal, Constants.FloatEpsilon) :
            //    point;
        }

        private static Vector3 GetAverageCollisionPointWithEpsilon(Collider collider1, Collider collider2, Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon = 1E-7)
        {
            double sqrEpsilon = epsilon * epsilon;

            Vector3[] GetFigureFromVertexes(List<Vector3> vertexes)
            {
                int count = vertexes.Count;
                Vector3[] figure = new Vector3[count];
                figure[0] = vertexes[0];
                vertexes.RemoveAt(0);

                Vector3 start = figure[0];
                Vector3 current;
                for (int k = 1; k < count - 1; k++)
                {
                    current = vertexes[0] - start;
                    int currentIndex = 0;
                    for (int i = 1; i < vertexes.Count; i++)
                    {
                        if ((vertexes[i] - start).cross(current) * collisionPlaneNormal > 0)
                        {
                            currentIndex = i;
                            current = vertexes[i] - start;
                        }
                    }

                    figure[k] = vertexes[currentIndex];
                    vertexes.RemoveAt(currentIndex);
                }

                if (vertexes.Count > 0)
                    figure[figure.Length - 1] = vertexes[0];

                return figure;
            }

            bool IsPointInsideFigure(Vector3[] figure, Vector3 point, out bool isPointOnEdge)
            {
                Vector3 start, end, edge;
                Vector3 vec, vecMult, prevVecMult;

                prevVecMult = (figure[1] - figure[0]).cross(point - figure[0]);
                for (int i = 0; i < figure.Length; i++)
                {
                    start = figure[i];
                    end = figure[(i + 1) % figure.Length];
                    edge = end - start;
                    vec = point - start;

                    vecMult = edge.cross(vec);
                    if (vecMult.squaredLength() < sqrEpsilon)
                    {
                        double edgeDotMult = edge.dot(edge);
                        double currentDotMult = vec.dot(edge);

                        if (currentDotMult >= 0 && currentDotMult <= edgeDotMult)
                        {
                            isPointOnEdge = true;
                            return true;
                        }
                        continue;
                    }

                    if (vecMult.dot(prevVecMult) < 0)
                    {
                        isPointOnEdge = false;
                        return false;
                    }

                    prevVecMult = vecMult;
                }

                isPointOnEdge = false;
                return true;
            }

            List<Vector3> vertexesOnPlane1 = collider1.GetVertexesOnPlane(collisionPlanePoint, collisionPlaneNormal, epsilon);
            List<Vector3> vertexesOnPlane2 = collider2.GetVertexesOnPlane(collisionPlanePoint, collisionPlaneNormal, epsilon);

            if (vertexesOnPlane1.Count == 0 || vertexesOnPlane2.Count == 0)
                throw new ArgumentException("Colliders don't intersect in the given plane.");

            if (vertexesOnPlane1.Count == 1)
                return vertexesOnPlane1[0];
            if (vertexesOnPlane2.Count == 1)
                return vertexesOnPlane2[0];

            Vector3[] figure1 = GetFigureFromVertexes(vertexesOnPlane1);
            Vector3[] figure2 = GetFigureFromVertexes(vertexesOnPlane2);

            List<Vector3> intersectionPoints = new List<Vector3>();

            bool pointOnEdge;
            bool pointExists;
            void AddIntersectionPoints(ref Vector3[] points1, ref Vector3[] points2)
            {
                foreach (Vector3 point in points1)
                {
                    if (IsPointInsideFigure(points2, point, out pointOnEdge))
                    {
                        if (!pointOnEdge)
                        {
                            intersectionPoints.Add(point);
                            continue;
                        }

                        pointExists = false;
                        foreach (Vector3 intersectionPoint in intersectionPoints)
                        {
                            if (intersectionPoint.equals(point))
                            {
                                pointExists = true;
                                break;
                            }
                        }
                        if (!pointExists)
                            intersectionPoints.Add(point);
                    }
                }
            }
            AddIntersectionPoints(ref figure1, ref figure2);
            AddIntersectionPoints(ref figure2, ref figure1);
            Vector3 start1, end1, start2, end2;
            Vector3 start1start2, start1end2;
            Vector3 edge1, edge2;
            Vector3 start2proj, end2proj;
            for (int i = 0; i < figure1.Length; i++)
            {
                start1 = figure1[i];
                end1 = figure1[(i + 1) % figure1.Length];
                edge1 = end1 - start1;
                for (int j = 0; j < figure2.Length; j++)
                {
                    start2 = figure2[j];
                    end2 = figure2[(j + 1) % figure2.Length];
                    edge2 = end2 - start2;

                    start1start2 = start2 - start1;
                    start1end2 = end2 - start1;
                    if (start1start2.cross(edge1).dot(start1end2.cross(edge1)) >= -sqrEpsilon ||
                        (-start1start2).cross(edge2).dot((end1 - start2).cross(edge2)) >= -sqrEpsilon)
                        continue;

                    start2proj = start1 + start1start2.projectOnVector(edge1);
                    end2proj = start1 + start1end2.projectOnVector(edge1);

                    double k = (start2 - start2proj).length();
                    k = k / (k + (end2 - end2proj).length());

                    // start2 is reused as variable for final point
                    start2 = start2proj * k + end2proj * (1.0 - k);

                    pointExists = false;
                    foreach (Vector3 intersectionPoint in intersectionPoints)
                    {
                        if (intersectionPoint.equals(start2))
                        {
                            pointExists = true;
                            break;
                        }
                    }
                    if (!pointExists)
                        intersectionPoints.Add(start2);
                }
            }

            double x = 0, y = 0, z = 0;
            foreach (Vector3 point in intersectionPoints)
            {
                x += point.x;
                y += point.y;
                z += point.z;
            }

            return new Vector3(x / intersectionPoints.Count, y / intersectionPoints.Count, z / intersectionPoints.Count);
        }

        #region SAT

        protected bool GetCollisionExitVector_SAT(Collider other, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            collisionExitVector = null;
            exitDirectionVector = null;
            colliderEndPoint = null;

            if (!IsOuterSphereIntersectWith(other))
                return false;

            List<Vector3> axes = GetPossibleCollisionDirections(this, other);

            Vector3[] projection = new Vector3[2];
            Vector3[] projectionOther = new Vector3[2];
            foreach (Vector3 axis in axes)
            {
                GetBoundaryPointsInDirection(axis, out projection[0], out projection[1]);
                other.GetBoundaryPointsInDirection(axis, out projectionOther[0], out projectionOther[1]);

                projection[0] = projection[0].projectOnVector(axis);
                projection[1] = projection[1].projectOnVector(axis);
                projectionOther[0] = projectionOther[0].projectOnVector(axis);
                projectionOther[1] = projectionOther[1].projectOnVector(axis);

                Vector3 segmentVector = projection[1] - projection[0];
                Vector3 outVec1 = projectionOther[0] - projection[1];
                Vector3 outVec2 = projectionOther[1] - projection[0];
                double t1 = outVec1 * segmentVector;
                double t2 = outVec2 * segmentVector;

                if (t1 > 0 || t2 < 0)
                    return false;

                Vector3 outVec;
                Vector3 newEndPoint;
                if (outVec1.squaredLength() > outVec2.squaredLength())
                {
                    newEndPoint = projection[0];
                    outVec = outVec2;
                }
                else
                {
                    newEndPoint = projection[1];
                    outVec = outVec1;
                }

                if (!collisionExitVector.HasValue || outVec.squaredLength() < collisionExitVector.Value.squaredLength())
                {
                    colliderEndPoint = newEndPoint;
                    collisionExitVector = outVec;
                    exitDirectionVector = segmentVector;
                }
            }

            return true;
        }

        private bool IsOuterSphereIntersectWith(Collider collider)
        {
            Vector3 centersVector = collider.GlobalCenter - GlobalCenter;
            return centersVector.squaredLength() <= SquaredOuterSphereRadius + 2 * OuterSphereRadius * collider.OuterSphereRadius + collider.SquaredOuterSphereRadius;
        }

        private static List<Vector3> GetPossibleCollisionDirections(Collider col1, Collider col2, bool first = true)
        {
            List<Vector3> result = new List<Vector3>();

            void AddUnique(Vector3 vec)
            {
                bool exists = false;
                foreach (Vector3 vector in result)
                {
                    if (vector.isCollinearTo(vec))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    result.Add(vec);
            }

            switch (col1)
            {
                case MeshCollider mesh1:
                    switch (col2)
                    {
                        case MeshCollider mesh2:
                            {
                                result.AddRange(mesh1.GlobalNonCollinearNormals);
                                foreach (Vector3 vec in mesh2.GlobalNonCollinearNormals)
                                    AddUnique(vec);
                                IReadOnlyList<Vector3> globalVertexes1 = mesh1.GlobalVertexes;
                                IReadOnlyList<Vector3> globalVertexes2 = mesh2.GlobalVertexes;

                                IReadOnlyList<(int a, int b)> edges2 = mesh2.Edges;
                                foreach ((int a, int b) edge1 in mesh1.Edges)
                                {
                                    foreach ((int a, int b) edge2 in edges2)
                                        AddUnique((globalVertexes1[edge1.b] - globalVertexes1[edge1.a]).cross(globalVertexes2[edge2.b] - globalVertexes2[edge2.a]));
                                }

                                return result;
                            }
                        case SphereCollider sphere2:
                            {
                                result.AddRange(mesh1.GlobalNonCollinearNormals);

                                IReadOnlyList<Vector3> globalVertexes1 = mesh1.GlobalVertexes;
                                Vector3 curAxis;
                                foreach (Vector3 vertex in globalVertexes1)
                                    AddUnique(vertex - sphere2.GlobalCenter);

                                foreach ((int a, int b) edge1 in mesh1.Edges)
                                {
                                    curAxis = globalVertexes1[edge1.b] - globalVertexes1[edge1.a];
                                    curAxis = curAxis.vecMul(sphere2.GlobalCenter - globalVertexes1[edge1.a]).vecMul(curAxis);
                                    AddUnique(curAxis);
                                }

                                return result;
                            }
                    }
                    break;
                case SphereCollider sphere1:
                    switch (col2)
                    {
                        case SphereCollider sphere2:
                            Vector3 tmp = sphere2.GlobalCenter - sphere1.GlobalCenter;
                            if (tmp.isZero())
                                result.Add(Vector3.UnitX);
                            else
                                result.Add(tmp);
                            return result;
                    }
                    break;
            }

            if (first)
                return GetPossibleCollisionDirections(col2, col1, false);

            throw new NotImplementedException("getPossibleCollisionDirections is not implemented for " + col1.GetType().Name + " and " + col2.GetType().Name + " pair.");
        }

        #endregion SAT

        #region GJK_EPA

        protected bool GetCollisionExitVector_GJK_EPA(Collider other, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            collisionExitVector = null;
            exitDirectionVector = null;
            colliderEndPoint = null;

            List<Vector3> simplex;

            if (!GilbertJohnsonKeerthi(other, out simplex))
                return false;

            ExpandingPolytopeAlgorithm(other, simplex, out collisionExitVector, out exitDirectionVector, out colliderEndPoint);

            return true;
        }

        private bool GilbertJohnsonKeerthi(Collider other, out List<Vector3> simplex)
        {
            simplex = new List<Vector3>();

            Vector3 A, B, C, D;
            Vector3 direction = Vector3.Forward;
            Vector3 point1, point2;
            Vector3 MinkowskiDifference()
            {
                GetBoundaryPointsInDirection(direction, out _, out point1);
                other.GetBoundaryPointsInDirection(direction, out point2, out _);
                return point1 - point2;
            }

            Vector3 tmp;

            A = MinkowskiDifference();

            if (A.isZero())
                direction = -Vector3.Forward;
            else
                direction = -A;
            B = MinkowskiDifference();

            if (direction.dot(B) <= -Constants.SqrEpsilon)
                return false;

            if ((tmp = A.cross(B - A)).isZero())
            {
                direction = Vector3.Up;
                C = MinkowskiDifference();
                if ((C - A).isCollinearTo(B - A))
                {
                    direction = -Vector3.Up;
                    C = MinkowskiDifference();
                }
            }
            else
            {
                direction = tmp.cross(B - A);
                C = MinkowskiDifference();
            }

            if (direction.dot(C) <= -Constants.SqrEpsilon)
                return false;

            if (Math.Abs((tmp = (B - A).cross(C - A)).dot(A)) <= Constants.SqrEpsilon)
            {
                direction = tmp;
                D = MinkowskiDifference();
                if (Math.Abs((D - A).dot(direction)) <= Constants.SqrEpsilon)
                {
                    direction = -tmp;
                    D = MinkowskiDifference();
                }
            }
            else
            {
                direction = tmp;
                if (direction.dot(A) > Constants.SqrEpsilon)
                    direction = -direction;
                D = MinkowskiDifference();
            }

            if (direction.dot(D) <= -Constants.SqrEpsilon)
                return false;

            if ((B - A).cross(C - A).dot(D - A) < -Constants.SqrEpsilon)
            {
                tmp = A;
                A = B;
                B = tmp;
            }

            while (true)
            {
                // checking face ABD
                direction = (B - A).cross(D - A);
                if (direction.dot(A) < -Constants.SqrEpsilon)
                {
                    tmp = MinkowskiDifference();

                    if (direction.dot(tmp) < -Constants.SqrEpsilon)
                        return false;

                    C = D;
                    D = tmp;
                    continue;
                }

                // checking face BCD
                direction = (C - B).cross(D - B);
                if (direction.dot(B) < -Constants.SqrEpsilon)
                {
                    tmp = MinkowskiDifference();

                    if (direction.dot(tmp) < -Constants.SqrEpsilon)
                        return false;

                    A = D;
                    D = tmp;
                    continue;
                }

                // checking face CAD
                direction = (A - C).cross(D - C);
                if (direction.dot(C) < -Constants.SqrEpsilon)
                {
                    tmp = MinkowskiDifference();

                    if (direction.dot(tmp) < -Constants.SqrEpsilon)
                        return false;

                    B = D;
                    D = tmp;
                    continue;
                }

                simplex = new List<Vector3>() { A, B, C, D };
                return true;
            }
        }

        private void ExpandingPolytopeAlgorithm(Collider other, List<Vector3> initialSimplex, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            Vector3 point1, point2;
            Vector3 direction;
            Vector3 MinkowskiDifference()
            {
                GetBoundaryPointsInDirection(direction, out _, out point1);
                other.GetBoundaryPointsInDirection(direction, out point2, out _);
                return point1 - point2;
            }

            SortedList<double, Polygon> polygons = new SortedList<double, Polygon>(distanceComparer);

            void AddPolygon(Polygon polygon)
            {
                polygon.normal = (initialSimplex[polygon.indexB] - initialSimplex[polygon.indexA]).cross(initialSimplex[polygon.indexC] - initialSimplex[polygon.indexA]);
                polygons.Add(initialSimplex[polygon.indexA].projectOnVector(polygon.normal).squaredLength(), polygon);
            }

            {
                Polygon bac = new Polygon(1, 0, 2);
                Polygon abd = new Polygon(0, 1, 3);
                Polygon cdb = new Polygon(2, 3, 1);
                Polygon dca = new Polygon(3, 2, 0);

                bac.adjacentAB = abd;
                abd.adjacentAB = bac;

                bac.adjacentBC = dca;
                dca.adjacentBC = bac;

                bac.adjacentCA = cdb;
                cdb.adjacentCA = bac;

                abd.adjacentBC = cdb;
                cdb.adjacentBC = abd;

                abd.adjacentCA = dca;
                dca.adjacentCA = abd;

                cdb.adjacentAB = dca;
                dca.adjacentAB = cdb;

                AddPolygon(bac);
                AddPolygon(abd);
                AddPolygon(cdb);
                AddPolygon(dca);
            }

            Polygon currentPolygon;
            Vector3 tmp;
            int tmpIndex;
            Polygon adjLeft, adjRight;
            int adjVertexIndex;
            int i = 0;
            List<int> hole = new List<int>();
            List<Polygon> adjacentPolygons = new List<Polygon>();
            List<Polygon> polygonsToReplace = new List<Polygon>();
            List<Polygon> newPolygons = new List<Polygon>();

            void CreateHolePart(Polygon adjacentPolygon, int indexLeft, int indexRight)
            {
                if ((tmp - initialSimplex[adjacentPolygon.indexA]).dot(adjacentPolygon.normal) >= -Constants.SqrEpsilon)
                {
                    Polygon.GetAdjacentPolygons(adjacentPolygon, indexLeft, indexRight, out adjLeft, out adjRight, out adjVertexIndex);
                    hole.Add(indexLeft);
                    hole.Add(adjVertexIndex);
                    adjacentPolygons.Add(adjLeft);
                    adjacentPolygons.Add(adjRight);
                    polygonsToReplace.Add(adjacentPolygon);
                    polygonsToReplace.Add(adjacentPolygon);

                    polygons.RemoveAt(polygons.IndexOfValue(adjacentPolygon));
                }
                else
                {
                    hole.Add(indexLeft);
                    adjacentPolygons.Add(adjacentPolygon);
                    polygonsToReplace.Add(currentPolygon);
                }
            }

            do
            {
                currentPolygon = polygons.Values[polygons.Count - 1];
                polygons.RemoveAt(polygons.Count - 1);

                direction = currentPolygon.normal;
                tmp = MinkowskiDifference();
                if ((tmp - initialSimplex[currentPolygon.indexA]).dot(direction) <= Constants.SqrEpsilon)
                    break;

                initialSimplex.Add(tmp);
                tmpIndex = initialSimplex.Count - 1;

                hole.Clear();
                adjacentPolygons.Clear();
                polygonsToReplace.Clear();
                newPolygons.Clear();

                CreateHolePart(currentPolygon.adjacentAB, currentPolygon.indexA, currentPolygon.indexB);
                CreateHolePart(currentPolygon.adjacentBC, currentPolygon.indexB, currentPolygon.indexC);
                CreateHolePart(currentPolygon.adjacentCA, currentPolygon.indexC, currentPolygon.indexA);

                for (int j = 1; j < hole.Count - 2; j++)
                {
                    if (hole[j] == hole[j + 2])
                    {
                        hole.RemoveRange(j, 2);
                        adjacentPolygons.RemoveRange(j, 2);
                        polygonsToReplace.RemoveRange(j, 2);
                    }
                }

                for (int j = 0; j < hole.Count; j++)
                {
                    Polygon polygon = new Polygon(hole[j], hole[(j + 1) % hole.Count], tmpIndex);
                    polygon.adjacentAB = adjacentPolygons[j];
                    Polygon.ReplaceAdjacent(adjacentPolygons[j], polygonsToReplace[j], polygon);
                    AddPolygon(polygon);
                    newPolygons.Add(polygon);
                }

                for (int j = 0; j < newPolygons.Count; j++)
                {
                    newPolygons[j].adjacentBC = newPolygons[(j + 1) % newPolygons.Count];
                    newPolygons[(j + 1) % newPolygons.Count].adjacentCA = newPolygons[j];
                }

                i++;
            } while (i < EpaMaxIter);

            if (i == EpaMaxIter)
            {
                currentPolygon = polygons.Values[polygons.Count - 1];

                direction = currentPolygon.normal;
                tmp = MinkowskiDifference();
            }

            tmp = -tmp.projectOnVector(direction);

            exitDirectionVector = direction;
            collisionExitVector = tmp;
            Vector3 _colliderEndPoint;
            GetBoundaryPointsInDirection(direction, out _, out _colliderEndPoint);
            colliderEndPoint = _colliderEndPoint;
        }

        #endregion GJK_EPA

        private class Polygon
        {
            public int indexA, indexB, indexC;
            public Polygon adjacentAB, adjacentBC, adjacentCA;
            public Vector3 normal;

            public Polygon(int indexA, int indexB, int indexC)
            {
                this.indexA = indexA;
                this.indexB = indexB;
                this.indexC = indexC;
            }

            public static void ReplaceAdjacent(Polygon polygon, Polygon toReplace, Polygon replaceFor)
            {
                if (polygon.adjacentAB == toReplace)
                    polygon.adjacentAB = replaceFor;
                else if (polygon.adjacentBC == toReplace)
                    polygon.adjacentBC = replaceFor;
                else if (polygon.adjacentCA == toReplace)
                    polygon.adjacentCA = replaceFor;
            }

            public static void GetAdjacentPolygons(Polygon polygon, int indexLeft, int indexRight, out Polygon adjacentLeft, out Polygon adjacentRight, out int thirdVertexIndex)
            {
                if (polygon.indexA != indexLeft && polygon.indexA != indexRight)
                {
                    thirdVertexIndex = polygon.indexA;
                    if (polygon.indexB == indexLeft)
                    {
                        adjacentLeft = polygon.adjacentAB;
                        adjacentRight = polygon.adjacentCA;
                    }
                    else
                    {
                        adjacentLeft = polygon.adjacentCA;
                        adjacentRight = polygon.adjacentAB;
                    }
                }
                else if (polygon.indexB != indexLeft && polygon.indexB != indexRight)
                {
                    thirdVertexIndex = polygon.indexB;
                    if (polygon.indexA == indexLeft)
                    {
                        adjacentLeft = polygon.adjacentAB;
                        adjacentRight = polygon.adjacentBC;
                    }
                    else
                    {
                        adjacentLeft = polygon.adjacentBC;
                        adjacentRight = polygon.adjacentAB;
                    }
                }
                else
                {
                    thirdVertexIndex = polygon.indexC;
                    if (polygon.indexA == indexLeft)
                    {
                        adjacentLeft = polygon.adjacentCA;
                        adjacentRight = polygon.adjacentBC;
                    }
                    else
                    {
                        adjacentLeft = polygon.adjacentBC;
                        adjacentRight = polygon.adjacentCA;
                    }
                }
            }
        }

        private class PolygonDistanceComparer : IComparer<double>
        {
            public int Compare(double x, double y)
            {
                int value = y.CompareTo(x);
                return value == 0 ? -1 : value;
            }
        }
    }
}