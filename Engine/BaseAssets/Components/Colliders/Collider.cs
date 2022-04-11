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
        protected abstract Vector3[] getPossibleCollisionDirections(Collider other);

        protected bool IsOuterSphereIntersectWith(Collider collider)
        {
            Vector3 centersVector = collider.globalCenter - globalCenter;
            return centersVector.squaredLength() <= SquaredOuterSphereRadius + 2 * OuterSphereRadius * collider.OuterSphereRadius + collider.SquaredOuterSphereRadius;
        }

        public virtual bool getCollisionExitVector_SAT(Collider other, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            collisionExitVector = null;
            exitDirectionVector = null;
            colliderEndPoint = null;

            if(!IsOuterSphereIntersectWith(other))
                return false;

            List<Vector3> axises = new List<Vector3>();
            axises.AddRange(getPossibleCollisionDirections(other));
            foreach(var normal in other.getPossibleCollisionDirections(this))
                if(!axises.Exists(n => n.isCollinearTo(normal)))
                    axises.Add(normal);

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