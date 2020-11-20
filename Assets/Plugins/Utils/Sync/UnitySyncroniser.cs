using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Utils.Sync
{
    public class UnitySyncroniser : MonoBehaviour, ISyncer
    {
        public int ActionsRemaining => _actionQueue.Count;

        private ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

        private int _attemptsRemaining;

        private int _unityThreadId;

        private void Awake()
        {
            _unityThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private void Update()
        {
            _attemptsRemaining = _actionQueue.Count;
            while (_actionQueue.Count > 0 && _attemptsRemaining > 0)
            {
                if (_actionQueue.TryDequeue(out Action action))
                {
                    action();
                }
                else
                {
                    _attemptsRemaining--;
                }
            }
        }

        public void Sync(Action action)
        {
            if (Thread.CurrentThread.ManagedThreadId == _unityThreadId)
            {
                // already on the unity thread so no need to queue
                action();
            }
            else
            {
                _actionQueue.Enqueue(action);
            }
        }
    }
}
