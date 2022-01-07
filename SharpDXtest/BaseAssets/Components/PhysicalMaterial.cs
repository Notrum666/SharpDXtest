using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    public enum FrictionCombineMode
    {
        Minimum,
        Maximum,
        Multiply,
        Average,
        GeometryAverage
    }

    public enum BouncinessCombineMode
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

        public FrictionCombineMode FrictionCombineMode { get; set; }
        public BouncinessCombineMode BouncinessCombineMode { get; set; }

        public PhysicalMaterial(double friction, double bounciness, FrictionCombineMode frictionCombineMode = FrictionCombineMode.Average, 
                                                                    BouncinessCombineMode bouncinessCombineMode = BouncinessCombineMode.Average)
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
                case FrictionCombineMode.Minimum:
                    return Math.Min(Friction, material.Friction);
                case FrictionCombineMode.Maximum:
                    return Math.Max(Friction, material.Friction);
                case FrictionCombineMode.Multiply:
                    return Friction * material.Friction;
                case FrictionCombineMode.Average:
                    return (Friction + material.Friction) * 0.5;
                case FrictionCombineMode.GeometryAverage:
                    return Math.Sqrt(Friction * material.Friction);
                default:
                    throw new NotImplementedException($"{FrictionCombineMode} friction combine mode doesn't implemented!");
            }
        }

        public double GetComdinedBouncinessWith(PhysicalMaterial material)
        {
            switch (BouncinessCombineMode)
            {
                case BouncinessCombineMode.Minimum:
                    return Math.Min(Bounciness, material.Bounciness);
                case BouncinessCombineMode.Maximum:
                    return Math.Max(Bounciness, material.Bounciness);
                case BouncinessCombineMode.Multiply:
                    return Bounciness * material.Bounciness;
                case BouncinessCombineMode.Average:
                    return (Bounciness + material.Bounciness) * 0.5;
                case BouncinessCombineMode.GeometryAverage:
                    return Math.Sqrt(Bounciness * material.Bounciness);
                default:
                    throw new NotImplementedException($"{BouncinessCombineMode} bounciness combine mode doesn't implemented!");
            }
        }
    }
}