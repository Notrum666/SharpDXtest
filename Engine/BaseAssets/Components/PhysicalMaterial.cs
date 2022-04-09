using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.BaseAssets.Components
{
    public enum CombineMode
    {
        Minimum,
        Maximum,
        Multiply,
        Average,
        GeometryAverage
    }

    public class PhysicalMaterial
    {
        private double friction;
        public double Friction
        {
            get
            {
                return friction;
            }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("Friction value must be in range from 0 to 1.");
                friction = value;
            }
        }

        private double bounciness;
        public double Bounciness
        {
            get
            {
                return bounciness;
            }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("Bounciness value must be in range from 0 to 1.");
                bounciness = value;
            }
        }

        public CombineMode FrictionCombineMode { get; set; }
        public CombineMode BouncinessCombineMode { get; set; }

        public PhysicalMaterial()
        {
            Friction = 0.5;
            Bounciness = 0.5;
            FrictionCombineMode = CombineMode.Average;
            BouncinessCombineMode = CombineMode.Average;
        }
        public PhysicalMaterial(double friction, double bounciness, CombineMode frictionCombineMode = CombineMode.Average,
                                                                    CombineMode bouncinessCombineMode = CombineMode.Average)
        {
            Friction = friction;
            Bounciness = bounciness;
            FrictionCombineMode = frictionCombineMode;
            BouncinessCombineMode = bouncinessCombineMode;
        }

        public double GetCombinedFrictionWith(PhysicalMaterial material)
        {
            switch(FrictionCombineMode)
            {
                case CombineMode.Minimum:
                    return Math.Min(Friction, material.Friction);
                case CombineMode.Maximum:
                    return Math.Max(Friction, material.Friction);
                case CombineMode.Multiply:
                    return Friction * material.Friction;
                case CombineMode.Average:
                    return (Friction + material.Friction) * 0.5;
                case CombineMode.GeometryAverage:
                    return Math.Sqrt(Friction * material.Friction);
                default:
                    throw new NotImplementedException($"{FrictionCombineMode} friction combine mode doesn't implemented!");
            }
        }

        public double GetComdinedBouncinessWith(PhysicalMaterial material)
        {
            switch (BouncinessCombineMode)
            {
                case CombineMode.Minimum:
                    return Math.Min(Bounciness, material.Bounciness);
                case CombineMode.Maximum:
                    return Math.Max(Bounciness, material.Bounciness);
                case CombineMode.Multiply:
                    return Bounciness * material.Bounciness;
                case CombineMode.Average:
                    return (Bounciness + material.Bounciness) * 0.5;
                case CombineMode.GeometryAverage:
                    return Math.Sqrt(Bounciness * material.Bounciness);
                default:
                    throw new NotImplementedException($"{BouncinessCombineMode} bounciness combine mode doesn't implemented!");
            }
        }
    }
}