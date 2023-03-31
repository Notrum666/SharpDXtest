using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.BaseAssets.Components
{
    public abstract class Component
    {
        private bool enabled = true;
        public bool Enabled { get => enabled && GameObject.Enabled; set => enabled = value; }
        private GameObject gameObject = null;
        public GameObject GameObject 
        { 
            get 
            { 
                return gameObject; 
            }
            set 
            { 
                if (gameObject != null) 
                    throw new Exception("gameObject can be set only once.");
                gameObject = value;
            } 
        }
        public bool IsInitialized { get; internal set; }
        public void Initialize()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;
            Initialized();
        }
        protected virtual void Initialized()
        {

        }
        public virtual void Update()
        {

        }
        public virtual void FixedUpdate()
        {

        }
    }
}
