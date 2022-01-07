using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    public enum FrictionCombineMode
    {
        Minimum = 1,
        Maximum = 2,
        Multiply = 3,
        Average = 4,
        GeometryAverage = 5
    }

    public enum BouncinessCombineMode
    {
        Minimum = 1,
        Maximum = 2,
        Multiply = 3,
        Average = 4,
        GeometryAverage = 5
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
                    throw new ArgumentOutOfRangeException("Friction value must be in [0; 1]!");
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
                    throw new ArgumentOutOfRangeException("Bounciness value must be in [0; 1]!");
                bounciness = value;
            }
        }

        public FrictionCombineMode FrictionCombineMode { get; set; }
        public BouncinessCombineMode BouncinessCombineMode { get; set; }

        public PhysicalMaterial(double friction, double bounciness, FrictionCombineMode frictionCombineMode, BouncinessCombineMode bouncinessCombineMode)
        {
            Friction = friction;
            Bounciness = bounciness;
            FrictionCombineMode = frictionCombineMode;
            BouncinessCombineMode = bouncinessCombineMode;
        }

        public double GetCombinedFrictionWith(PhysicalMaterial material)
        {
            switch(this.FrictionCombineMode)
            {
                case FrictionCombineMode.Minimum:
                    return Math.Min(this.Friction, material.Friction);
                case FrictionCombineMode.Maximum:
                    return Math.Max(this.Friction, material.Friction);
                case FrictionCombineMode.Multiply:
                    return this.Friction * material.Friction;
                case FrictionCombineMode.Average:
                    return (this.Friction + material.Friction) * 0.5;
                case FrictionCombineMode.GeometryAverage:
                    return Math.Sqrt(this.Friction * material.Friction);
                default:
                    throw new NotImplementedException($"{this.FrictionCombineMode} friction combine mode doesn't implemented!");
            }
        }

        public double GetComdinedBouncinessWith(PhysicalMaterial material)
        {
            switch (this.BouncinessCombineMode)
            {
                case BouncinessCombineMode.Minimum:
                    return Math.Min(this.Bounciness, material.Bounciness);
                case BouncinessCombineMode.Maximum:
                    return Math.Max(this.Bounciness, material.Bounciness);
                case BouncinessCombineMode.Multiply:
                    return this.Bounciness * material.Bounciness;
                case BouncinessCombineMode.Average:
                    return (this.Bounciness + material.Bounciness) * 0.5;
                case BouncinessCombineMode.GeometryAverage:
                    return Math.Sqrt(this.Bounciness * material.Bounciness);
                default:
                    throw new NotImplementedException($"{this.BouncinessCombineMode} bounciness combine mode doesn't implemented!");
            }
        }
    }
}