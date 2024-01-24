using Engine;
using Engine.BaseAssets.Components;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace TestProject
{
    public class TriggerLogger : BehaviourComponent
    {
        private double t = 0;
        public override void Start()
        {
            GameObject.GetComponent<Collider>().OnTriggerEnter += OnTriggerEnter;
            GameObject.GetComponent<Collider>().OnTriggerStay += OnTriggerStay;
            GameObject.GetComponent<Collider>().OnTriggerExit += OnTriggerExit;
            GameObject.GetComponent<Collider>().OnCollisionBegin += OnCollisionBegin;
            GameObject.GetComponent<Collider>().OnCollision += OnCollision;
            GameObject.GetComponent<Collider>().OnCollisionEnd += OnCollisionEnd;
        }

        private void OnCollisionEnd(Collider sender, Collider other)
        {
            Logger.Log(LogType.Info, $"OnCollisionEnd, sender: {sender.GameObject.Name}, other: {other.GameObject.Name}");
        }

        private void OnCollision(Collider sender, Collider other)
        {
            t += Time.DeltaTime;
            if (t >= 1.0)
            {
                t = 0;
                Logger.Log(LogType.Info, $"OnCollision, sender: {sender.GameObject.Name}, other: {other.GameObject.Name}");
            }
        }

        private void OnCollisionBegin(Collider sender, Collider other)
        {
            Logger.Log(LogType.Info, $"OnCollisionBegin, sender: {sender.GameObject.Name}, other: {other.GameObject.Name}");
        }

        private void OnTriggerStay(Collider sender, Collider other)
        {
            t += Time.DeltaTime;
            if (t >= 1.0)
            {
                t = 0;
                Logger.Log(LogType.Info, $"Trigger stay, sender: {sender.GameObject.Name}, other: {other.GameObject.Name}");
            }
        }

        private void OnTriggerExit(Collider sender, Collider other)
        {
            Logger.Log(LogType.Info, $"Trigger exit, sender: {sender.GameObject.Name}, other: {other.GameObject.Name}");
        }

        private void OnTriggerEnter(Collider sender, Collider other)
        {
            Logger.Log(LogType.Info, $"Trigger enter, sender: {sender.GameObject.Name}, other: {other.GameObject.Name}");
        }
    }
}