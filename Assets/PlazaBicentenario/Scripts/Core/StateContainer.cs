// Adaptado e integrado para SupervivenciaEnLaPaz a partir de la arquitectura de estados de SanAndreasUnity.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SupervivenciaEnLaPaz.Core
{
    /// <summary>
    /// Contenedor tipado de estados para consulta eficiente por tipo concreto o jerarquía.
    /// </summary>
    public sealed class StateContainer<TState> where TState : class, IState
    {
        private readonly List<TState> _states = new();
        private readonly Dictionary<Type, TState> _byType = new();

        public IReadOnlyList<TState> States => _states;

        public void AddState(TState state)
        {
            if (state == null)
                return;

            Type type = state.GetType();
            if (_byType.ContainsKey(type))
                return;

            _byType.Add(type, state);
            _states.Add(state);
        }

        public void AddStates(IEnumerable<TState> states)
        {
            if (states == null)
                return;

            foreach (TState state in states)
                AddState(state);
        }

        public TState GetState(Type type)
        {
            if (type == null)
                return null;

            if (_byType.TryGetValue(type, out TState exact))
                return exact;

            for (int i = 0; i < _states.Count; i++)
            {
                if (type.IsInstanceOfType(_states[i]))
                    return _states[i];
            }

            return null;
        }

        public T GetState<T>() where T : class, TState
        {
            return GetState(typeof(T)) as T;
        }

        public TState GetStateOrLogError(Type type)
        {
            TState state = GetState(type);
            if (state == null)
                Debug.LogError($"No se encontró el estado '{type?.Name ?? "null"}' en {nameof(StateContainer<TState>)}.");
            return state;
        }

        public T GetStateOrLogError<T>() where T : class, TState
        {
            return GetStateOrLogError(typeof(T)) as T;
        }

        public T GetStateOrThrow<T>() where T : class, TState
        {
            T state = GetState<T>();
            if (state == null)
                throw new InvalidOperationException($"No se encontró el estado '{typeof(T).Name}' en {nameof(StateContainer<TState>)}.");
            return state;
        }

        public List<TU> GetStatesThatInherit<TU>() where TU : class
        {
            var result = new List<TU>();
            for (int i = 0; i < _states.Count; i++)
            {
                if (_states[i] is TU match)
                    result.Add(match);
            }
            return result;
        }
    }
}
