using Engine.BaseAssets.Components.Colliders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public abstract class Collider : Component
    {
        private class PolygonDistanceComparer : IComparer<double>
        {
            public int Compare(double x, double y)
            {
                int value = y.CompareTo(x);
                return value == 0 ? -1 : value;
            }
        }
        private static readonly PolygonDistanceComparer distanceComparer = new PolygonDistanceComparer();
        private const int EPA_MAX_ITER = 4096;
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
        }
        public abstract Vector3 InertiaTensor { get; }

        public virtual Vector3 Offset { get; set; }
        private Vector3 globalCenter;
        public Vector3 GlobalCenter { get => globalCenter; }
        private double massPart = 1.0;
        public double MassPart
        {
            get
            {
                return massPart;
            }
            set
            {
                if (value < 0.0)
                    throw new ArgumentException("Mass part can't be negative.");
                massPart = value;
            }
        }

        public abstract double SquaredOuterSphereRadius { get; }
        public abstract double OuterSphereRadius { get; }

        protected abstract void getBoundaryPointsInDirection(Vector3 direction, out Vector3 hindmost, out Vector3 furthest);
        private static List<Vector3> getPossibleCollisionDirections(Collider col1, Collider col2, bool first = true)
        {
            List<Vector3> result = new List<Vector3>();
            
            void addUnique(Vector3 vec)
            {
                bool exists = false;
                foreach (Vector3 vector in result)
                    if (vector.isCollinearTo(vec))
                    {
                        exists = true;
                        break;
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
                                    addUnique(vec);
                                IReadOnlyList<Vector3> globalVertexes1 = mesh1.GlobalVertexes;
                                IReadOnlyList<Vector3> globalVertexes2 = mesh2.GlobalVertexes;

                                IReadOnlyList<(int a, int b)> edges2 = mesh2.Edges;
                                foreach ((int a, int b) edge1 in mesh1.Edges)
                                    foreach ((int a, int b) edge2 in edges2)
                                        addUnique((globalVertexes1[edge1.b] - globalVertexes1[edge1.a]).cross(globalVertexes2[edge2.b] - globalVertexes2[edge2.a]));

                                return result;
                            }
                        case SphereCollider sphere2:
                            {
                                result.AddRange(mesh1.GlobalNonCollinearNormals);

                                IReadOnlyList<Vector3> globalVertexes1 = mesh1.GlobalVertexes;
                                Vector3 curAxis;
                                foreach (Vector3 vertex in globalVertexes1)
                                    addUnique(vertex - sphere2.globalCenter);

                                foreach ((int a, int b) edge1 in mesh1.Edges)
                                {
                                    curAxis = globalVertexes1[edge1.b] - globalVertexes1[edge1.a];
                                    curAxis = curAxis.vecMul(sphere2.globalCenter - globalVertexes1[edge1.a]).vecMul(curAxis);
                                    addUnique(curAxis);
                                }

                                return result;
                            }
                    }
                    break;
                case SphereCollider sphere1:
                    switch (col2)
                    {
                        case SphereCollider sphere2:
                            result.Add(sphere2.globalCenter - sphere1.globalCenter);
                            return result;
                    }
                    break;
            }


            if (first)
                return getPossibleCollisionDirections(col2, col1, false);

            throw new NotImplementedException("getPossibleCollisionDirections is not implemented for " + col1.GetType().Name + " and " + col2.GetType().Name + " pair.");
        }

        protected bool IsOuterSphereIntersectWith(Collider collider)
        {
            Vector3 centersVector = collider.globalCenter - globalCenter;
            return centersVector.squaredLength() <= SquaredOuterSphereRadius + 2 * OuterSphereRadius * collider.OuterSphereRadius + collider.SquaredOuterSphereRadius;
        }
        public bool getCollisionExitVector_SAT(Collider other, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            collisionExitVector = null;
            exitDirectionVector = null;
            colliderEndPoint = null;

            if(!IsOuterSphereIntersectWith(other))
                return false;

            List<Vector3> axises = getPossibleCollisionDirections(this, other);

            Vector3[] projection = new Vector3[2];
            Vector3[] projectionOther = new Vector3[2];
            foreach(Vector3 axis in axises)
            {
                getBoundaryPointsInDirection(axis, out projection[0], out projection[1]);
                other.getBoundaryPointsInDirection(axis, out projectionOther[0], out projectionOther[1]);

                projection[0] = projection[0].projectOnVector(axis);
                projection[1] = projection[1].projectOnVector(axis);
                projectionOther[0] = projectionOther[0].projectOnVector(axis);
                projectionOther[1] = projectionOther[1].projectOnVector(axis);

                Vector3 segmentVector = projection[1] - projection[0];
                double segmentVectorSqrLength = segmentVector.squaredLength();
                double t1 = (projectionOther[0] - projection[0]) * segmentVector;
                double t2 = (projectionOther[1] - projection[0]) * segmentVector;

                if (t2 < 0 || t1 > segmentVectorSqrLength)
                    return false;

                Vector3 outVec1 = projectionOther[0] - projection[1];
                Vector3 outVec2 = projectionOther[1] - projection[0];
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
        private bool GilbertJohnsonKeerthi(Collider other, out List<Vector3> simplex)
        {
            simplex = new List<Vector3>();

            Vector3 A, B, C, D;
            Vector3 direction = Vector3.Forward;
            Vector3 point1, point2;
            Vector3 MinkowskiDifference()
            {
                getBoundaryPointsInDirection(direction, out _, out point1);
                other.getBoundaryPointsInDirection(direction, out point2, out _);
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
                getBoundaryPointsInDirection(direction, out _, out point1);
                other.getBoundaryPointsInDirection(direction, out point2, out _);
                return point1 - point2;
            }

            SortedList<double, Polygon> polygons = new SortedList<double, Polygon>(distanceComparer);

            void addPolygon(Polygon polygon)
            {
                polygon.normal = (initialSimplex[polygon.indexB] - initialSimplex[polygon.indexA]).cross(initialSimplex[polygon.indexC] - initialSimplex[polygon.indexA]);
                if (polygon.normal.x == 0 && polygon.normal.y == 0 && polygon.normal.z == 0)
                {

                }
                polygons.Add(initialSimplex[polygon.indexA].projectOnVector(polygon.normal).squaredLength(), polygon);
            }

            {
                Polygon BAC = new Polygon(1, 0, 2);
                Polygon ABD = new Polygon(0, 1, 3);
                Polygon CDB = new Polygon(2, 3, 1);
                Polygon DCA = new Polygon(3, 2, 0);

                BAC.adjacentAB = ABD;
                ABD.adjacentAB = BAC;

                BAC.adjacentBC = DCA;
                DCA.adjacentBC = BAC;

                BAC.adjacentCA = CDB;
                CDB.adjacentCA = BAC;

                ABD.adjacentBC = CDB;
                CDB.adjacentBC = ABD;

                ABD.adjacentCA = DCA;
                DCA.adjacentCA = ABD;

                CDB.adjacentAB = DCA;
                DCA.adjacentAB = CDB;

                addPolygon(BAC);
                addPolygon(ABD);
                addPolygon(CDB);
                addPolygon(DCA);
            }

            void replaceAdjacent(Polygon polygon, Polygon toReplace, Polygon replaceFor)
            {
                if (polygon.adjacentAB == toReplace)
                    polygon.adjacentAB = replaceFor;
                else if (polygon.adjacentBC == toReplace)
                    polygon.adjacentBC = replaceFor;
                else if (polygon.adjacentCA == toReplace)
                    polygon.adjacentCA = replaceFor;
            }
            void getAdjacentPolygons(Polygon polygon, int indexLeft, int indexRight, out Polygon adjacentLeft, out Polygon adjacentRight, out int thirdVertexIndex)
            {
                if (polygon.indexA != indexLeft && polygon.indexA != indexRight)
                {
                    thirdVertexIndex = polygon.indexA;
                    if (polygon.indexB == indexLeft)
                    {
                        adjacentLeft = polygon.adjacentAB;
                        adjacentRight = polygon.adjacentCA;
                        return;
                    }
                    else
                    {
                        adjacentLeft = polygon.adjacentCA;
                        adjacentRight = polygon.adjacentAB;
                        return;
                    }
                }
                else if (polygon.indexB != indexLeft && polygon.indexB != indexRight)
                {
                    thirdVertexIndex = polygon.indexB;
                    if (polygon.indexA == indexLeft)
                    {
                        adjacentLeft = polygon.adjacentAB;
                        adjacentRight = polygon.adjacentBC;
                        return;
                    }
                    else
                    {
                        adjacentLeft = polygon.adjacentBC;
                        adjacentRight = polygon.adjacentAB;
                        return;
                    }
                }
                else
                {
                    thirdVertexIndex = polygon.indexC;
                    if (polygon.indexA == indexLeft)
                    {
                        adjacentLeft = polygon.adjacentCA;
                        adjacentRight = polygon.adjacentBC;
                        return;
                    }
                    else
                    {
                        adjacentLeft = polygon.adjacentBC;
                        adjacentRight = polygon.adjacentCA;
                        return;
                    }
                }
            }

            Polygon currentPolygon;
            Vector3 tmp;
            int tmpIndex;
            Polygon AB_left, AB_right, BC_left, BC_right, CA_left, CA_right;
            Polygon adjLeft, adjRight;
            int adjVertexIndex;
            int i = 0;
            void resolveCurrentPolygonSide(Polygon adjacentToResolve, int leftVertexIndex, int rightVertexIndex, out Polygon leftPolygonResult, out Polygon rightPolygonResult)
            {
                if ((tmp - initialSimplex[adjacentToResolve.indexA]).dot(adjacentToResolve.normal) >= -Constants.SqrEpsilon)
                {
                    getAdjacentPolygons(adjacentToResolve, leftVertexIndex, rightVertexIndex, out adjLeft, out adjRight, out adjVertexIndex);
                    leftPolygonResult = new Polygon(leftVertexIndex, adjVertexIndex, tmpIndex);
                    rightPolygonResult = new Polygon(adjVertexIndex, rightVertexIndex, tmpIndex);
                    leftPolygonResult.adjacentAB = adjLeft;
                    rightPolygonResult.adjacentAB = adjRight;
                    leftPolygonResult.adjacentBC = rightPolygonResult;
                    rightPolygonResult.adjacentCA = leftPolygonResult;
                    replaceAdjacent(adjLeft, adjacentToResolve, leftPolygonResult);
                    replaceAdjacent(adjRight, adjacentToResolve, rightPolygonResult);
                    polygons.RemoveAt(polygons.IndexOfValue(adjacentToResolve));
                    addPolygon(leftPolygonResult);
                    addPolygon(rightPolygonResult);
                }
                else
                {
                    rightPolygonResult = new Polygon(leftVertexIndex, rightVertexIndex, tmpIndex);
                    leftPolygonResult = rightPolygonResult;
                    rightPolygonResult.adjacentAB = adjacentToResolve;
                    replaceAdjacent(adjacentToResolve, currentPolygon, rightPolygonResult);
                    addPolygon(rightPolygonResult);
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

                resolveCurrentPolygonSide(currentPolygon.adjacentAB, currentPolygon.indexA, currentPolygon.indexB, out AB_left, out AB_right);
                resolveCurrentPolygonSide(currentPolygon.adjacentBC, currentPolygon.indexB, currentPolygon.indexC, out BC_left, out BC_right);
                resolveCurrentPolygonSide(currentPolygon.adjacentCA, currentPolygon.indexC, currentPolygon.indexA, out CA_left, out CA_right);

                AB_right.adjacentBC = BC_left;
                BC_left.adjacentCA = AB_right;

                BC_right.adjacentBC = CA_left;
                CA_left.adjacentCA = BC_right;

                CA_right.adjacentBC = AB_left;
                AB_left.adjacentCA = CA_right;

                i++;
            } while (i < EPA_MAX_ITER);

            if (i == EPA_MAX_ITER)
            {
                currentPolygon = polygons.Values[polygons.Count - 1];

                direction = currentPolygon.normal;
                tmp = MinkowskiDifference();
            }

            tmp = -tmp.projectOnVector(direction);

            exitDirectionVector = direction;
            collisionExitVector = tmp;
            Vector3 _colliderEndPoint;
            getBoundaryPointsInDirection(direction, out _, out _colliderEndPoint);
            colliderEndPoint = _colliderEndPoint;
        }
        public bool getCollisionExitVector_GJK_EPA(Collider other, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
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
        public bool getCollisionExitVector(Collider other, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            //return getCollisionExitVector_SAT(other, out collisionExitVector, out exitDirectionVector, out colliderEndPoint);
            return getCollisionExitVector_GJK_EPA(other, out collisionExitVector, out exitDirectionVector, out colliderEndPoint);
        }
        protected abstract List<Vector3> getVertexesOnPlane(Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon);

        private static Vector3 GetAverageCollisionPointWithEpsilon(Collider collider1, Collider collider2, Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon = 1E-7)
        {
            double sqrEpsilon = epsilon * epsilon;

            Vector3[] getFigureFromVertexes(List<Vector3> vertexes)
            {
                int count = vertexes.Count;
                Vector3[] figure = new Vector3[count];
                figure[0] = vertexes[0];
                vertexes.RemoveAt(0);

                Vector3 start = figure[0];
                Vector3 current;
                for(int k = 1; k < count - 1; k++)
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
                        else
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

            List<Vector3> vertexesOnPlane1 = collider1.getVertexesOnPlane(collisionPlanePoint, collisionPlaneNormal, epsilon);
            List<Vector3> vertexesOnPlane2 = collider2.getVertexesOnPlane(collisionPlanePoint, collisionPlaneNormal, epsilon);

            if (vertexesOnPlane1.Count == 0 || vertexesOnPlane2.Count == 0)
                throw new ArgumentException("Colliders don't intersect in the given plane.");

            if (vertexesOnPlane1.Count == 1)
                return vertexesOnPlane1[0];
            if (vertexesOnPlane2.Count == 1)
                return vertexesOnPlane2[0];

            Vector3[] figure1 = getFigureFromVertexes(vertexesOnPlane1);
            Vector3[] figure2 = getFigureFromVertexes(vertexesOnPlane2);

            List<Vector3> intersectionPoints = new List<Vector3>();

            bool pointOnEdge;
            bool pointExists;
            void addIntersectionPoints(ref Vector3[] points1, ref Vector3[] points2)
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
                            if (intersectionPoint.equals(point))
                            {
                                pointExists = true;
                                break;
                            }
                        if (!pointExists)
                            intersectionPoints.Add(point);
                    }
                }
            }
            addIntersectionPoints(ref figure1, ref figure2);
            addIntersectionPoints(ref figure2, ref figure1);
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
                        if (intersectionPoint.equals(start2))
                        {
                            pointExists = true;
                            break;
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

        public static Vector3 GetAverageCollisionPoint(Collider collider1, Collider collider2, Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal)
        {
            Vector3 point = GetAverageCollisionPointWithEpsilon(collider1, collider2, collisionPlanePoint, collisionPlaneNormal, Constants.Epsilon);
            return double.IsNaN(point.x) || double.IsNaN(point.y) || double.IsNaN(point.z) ?
                GetAverageCollisionPointWithEpsilon(collider1, collider2, collisionPlanePoint, collisionPlaneNormal, Constants.FloatEpsilon) :
                point;
        }

        public virtual void updateData()
        {
            globalCenter = (gameObject.transform.Model * new Vector4(Offset, 1)).xyz;
        }
        public override void fixedUpdate()
        {
            updateData();
        }
    }
}