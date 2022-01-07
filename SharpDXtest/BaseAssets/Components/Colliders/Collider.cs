using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    public abstract class Collider : Component
    {
        public Vector3 InertiaTensor { get; protected set; }

        public Vector3 Offset { get; set; }

        protected List<Vector3> vertices;
        protected List<Vector3> globalSpaceVertices;
        protected List<Vector3> globalSpaceNormals;
        protected List<int[]> polygons;

        protected double squaredOuterSphereRadius;
        protected double outerSphereRadius;

        protected void calculateOuterSphereRadius()
        {
            double currentLenght;

            foreach(Vector3 vertex in vertices)
            {
                currentLenght = vertex.squaredLength();
                if(currentLenght > squaredOuterSphereRadius)
                {
                    squaredOuterSphereRadius = currentLenght;
                    outerSphereRadius = Math.Sqrt(squaredOuterSphereRadius);
                }
            }
        }
        protected void convertLocalVerticesToGlobal()
        {
            globalSpaceVertices.Clear();

            Matrix4x4 worldMatrix = gameObject.transform.model;
            foreach(Vector3 localVertex in vertices)
            {
                globalSpaceVertices.Add((worldMatrix * new Vector4(localVertex, 1)).xyz);
            }
        }
        protected void calculateNormalsInGlobal()
        {
            globalSpaceNormals.Clear();

            Vector3 currentNormal;
            Vector3 edge1;
            Vector3 edge2;
            foreach(int[] poly in polygons)
            {
                edge1 = globalSpaceVertices[poly[1]] - globalSpaceVertices[poly[0]];
                edge2 = globalSpaceVertices[poly[2]] - globalSpaceVertices[poly[1]];
                currentNormal = edge1.cross(edge2);

                if (!globalSpaceNormals.Exists(n => n.isCollinearTo(currentNormal)))
                    globalSpaceNormals.Add(currentNormal);
            }
        }
        protected List<Vector3> projectOnVector(Vector3 vector)
        {
            List<Vector3> projection = new List<Vector3>();

            foreach(Vector3 vertex in vertices)
            {
                projection.Add(vertex.projectOnVector(vector));
            }

            return projection;
        }
        protected Vector3 getCenterInGlobal()
        {
            return (gameObject.transform.model * new Vector4(this.Offset, 1)).xyz;
        }
        private static Vector3[] getSegmentFromProjection(List<Vector3> projection)
        {
            Vector3[] segment = new Vector3[2]; // min, max

            int i;
            for (i = 1; i < projection.Count; i++)
            {
                if (!(projection[i] - projection[0]).isZero())
                    break;
            }

            double maxK = 0, minK = 0, k;
            Vector3 baseVector = projection[i] - projection[0];

            for (i = 1; i < projection.Count; i++)
            {
                k = (projection[i] - projection[0]) * baseVector;

                if (k > maxK)
                {
                    maxK = k;
                    segment[1] = projection[i];
                }
                else if (k < minK)
                {
                    minK = k;
                    segment[0] = projection[i];
                }
            }

            if (minK == 0)
                segment[0] = projection[0];

            if (maxK == 0)
                segment[1] = projection[0];

            return segment;
        }

        public virtual bool GetCollisionExitVector(Collider collider, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            collisionExitVector = null;
            exitDirectionVector = null;
            colliderEndPoint = null;

            Vector3 centersVector = collider.getCenterInGlobal() - this.getCenterInGlobal();
            if(centersVector.squaredLength() > squaredOuterSphereRadius + 2 * outerSphereRadius * collider.outerSphereRadius + collider.squaredOuterSphereRadius)
            {
                return false;
            }

            List<Vector3> normals = new List<Vector3>();
            normals.AddRange(this.globalSpaceNormals);
            foreach(var normal in collider.globalSpaceNormals)
            {
                if(!normals.Exists(n => n.isCollinearTo(normal)))
                {
                    normals.Add(normal);
                }
            }

            Vector3 edge1, edge2, currentNormal;
            foreach(int[] poly1 in this.polygons)
            {
                foreach(int[] poly2 in collider.polygons)
                {
                    for(int i = 0; i < poly1.Length; i++)
                    {
                        edge1 = globalSpaceVertices[poly1[(i + 1) % poly1.Length]] - globalSpaceVertices[poly1[i]];

                        for (int j = 0; j < poly2.Length; j++)
                        {
                            edge2 = collider.globalSpaceVertices[poly2[(j + 1) % poly2.Length]] - collider.globalSpaceVertices[poly2[j]];
                            currentNormal = edge1.cross(edge2);

                            if(!normals.Exists(n => n.isCollinearTo(currentNormal)))
                            {
                                normals.Add(currentNormal);
                            }
                        }
                    }
                }
            }

            foreach(Vector3 normal in normals)
            {
                List<Vector3> projection1 = this.projectOnVector(normal);
                List<Vector3> projection2 = collider.projectOnVector(normal);

                Vector3[] segment1 = getSegmentFromProjection(projection1);
                Vector3[] segment2 = getSegmentFromProjection(projection2);

                if ((segment2[1] - segment2[0]) * (segment1[1] - segment1[0]) < 0)
                {
                    Vector3 tmp = segment2[1];
                    segment2[1] = segment2[0];
                    segment2[0] = tmp;
                }

                Vector3 segmentVector = segment1[1] - segment1[0];
                double segmentVectorSqrLength = segmentVector.squaredLength();
                double t1 = (segment2[0] - segment1[0]) * segmentVector;
                double t2 = (segment2[1] - segment1[0]) * segmentVector;

                if (t2 < 0 || t1 > segmentVectorSqrLength)
                    return false;

                Vector3 outVec1 = segment2[0] - segment1[1];
                Vector3 outVec2 = segment2[1] - segment1[0];
                Vector3 outVec;
                Vector3 newEndPoint;
                if (outVec1.squaredLength() > outVec2.squaredLength())
                {
                    newEndPoint = segment1[0];
                    outVec = outVec2;
                }
                else
                {
                    newEndPoint = segment1[1];
                    outVec = outVec1;
                }

                if (!collisionExitVector.HasValue || outVec.squaredLength() < collisionExitVector.Value.squaredLength())
                {
                    colliderEndPoint = newEndPoint;
                    collisionExitVector = outVec;
                    exitDirectionVector = outVec;
                }

                /*
                Vector3 segmentVector = segment1[1] - segment1[0];
                Vector3 start1ToStart2 = segment2[0] - segment1[0];
                Vector3 start1ToEnd2 = segment2[1] - segment1[0];

                double t1 = start1ToStart2 * segmentVector;
                double t2 = start1ToEnd2 * segmentVector;
                double segmentVectorSqrLength = segmentVector.squaredLength();

                if(t1 > segmentVectorSqrLength && t2 > segmentVectorSqrLength ||
                   t1 < 0 && t2 < 0)
                {
                    return false;
                }

                if(Math.Abs(t1) < Constants.Epsilon && t2 < 0 ||
                   Math.Abs(t2) < Constants.Epsilon && t1 < 0)
                {
                    collisionExitVector = Vector3.Zero;
                    exitDirectionVector = segmentVector;
                }
                else if(Math.Abs(t1 - 1) < Constants.Epsilon && t2 > 1 ||
                        Math.Abs(t2 - 1) < Constants.Epsilon && t1 > 1)
                {
                    collisionExitVector = Vector3.Zero;
                    exitDirectionVector = segmentVector;
                }
                else
                {
                    if(t1 > t2)
                    {
                        Vector3 tmp = segment2[0];
                        segment2[0] = segment2[1];
                        segment2[1] = tmp;
                    }

                    Vector3 v1 = segment2[1] - segment1[0];
                    Vector3 v2 = segment1[1] - segment2[0];

                    if (v2.squaredLength() > v1.squaredLength())
                        v1 = v2;

                    if(!collisionExitVector.HasValue || v1.squaredLength() < collisionExitVector.Value.squaredLength())
                    {
                        collisionExitVector = v1;
                        exitDirectionVector = v1;
                    }
                }
                */
            }

            return true;
        }
    }
}