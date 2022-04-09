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
        public bool Enabled { get => enabled && gameObject.Enabled; set => enabled = value; }
        private GameObject _gameObject = null;
        public GameObject gameObject 
        { 
            get 
            { 
                return _gameObject; 
            }
            set 
            { 
                if (_gameObject != null) 
                    throw new Exception("gameObject can be set only once.");
                _gameObject = value;
            } 
        }
        public virtual void update()
        {

        }
        public virtual void fixedUpdate()
        {

        }
    }
}
