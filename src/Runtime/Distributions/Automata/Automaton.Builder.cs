// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ML.Probabilistic.Distributions.Automata
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ML.Probabilistic.Core.Collections;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Utilities;

    public abstract partial class Automaton<TSequence, TElement, TElementDistribution, TSequenceManipulator, TThis>
        where TSequence : class, IEnumerable<TElement>
        where TElementDistribution : IDistribution<TElement>, SettableToProduct<TElementDistribution>,
        SettableToWeightedSumExact<TElementDistribution>, CanGetLogAverageOf<TElementDistribution>,
        SettableToPartialUniform<TElementDistribution>, new()
        where TSequenceManipulator : ISequenceManipulator<TSequence, TElement>, new()
        where TThis : Automaton<TSequence, TElement, TElementDistribution, TSequenceManipulator, TThis>, new()
    {
        public class Builder
        {
            private readonly List<StateData> states;
            private readonly List<LinkedTransition> transitions;

            public Builder()
            {
                this.states = new List<StateData>();
                this.transitions = new List<LinkedTransition>();
            }

            public int StartStateIndex { get; set; }

            public int StatesCount => states.Count;

            public StateBuilder this[int index] => new StateBuilder(this, index);

            public StateBuilder Start => this[this.StartStateIndex];

            public static Builder Zero()
            {
                var builder = new Builder();
                builder.AddState();
                return builder;
            }

            public static Builder FromAutomaton(Automaton<TSequence, TElement, TElementDistribution, TSequenceManipulator, TThis> automaton)
            {
                throw new NotImplementedException();
            }

            public static Builder ConstantOn(double weight, TSequence sequence)
            {
                throw new NotImplementedException();
            }

            public StateBuilder AddState()
            {
                var index = this.states.Count;
                this.states.Add(new StateData { FirstTransition = -1 });
                return new StateBuilder(this, index);
            }

            public StateBuilder GetState(int index) => new StateBuilder(this, index);

            public void AddStates(int count)
            {
                for (var i = 0; i < count; ++i)
                {
                    AddState();
                }
            }

            public void AddStates(StateCollection states)
            {
                throw new NotImplementedException();
            }

            public void Append(TThis automaton, bool avoidEpsilonTransitions = true)
            {
                throw new NotImplementedException();
                /*
                 * 
        if (ReferenceEquals(automaton, this))
            {
                automaton = automaton.Clone();
            }

            // Append the states of the second automaton
            var endStates = this.States.Where(nd => nd.CanEnd).ToList();
            int stateCount = this.States.Count;

            this.States.Append(automaton.States, group);
            var secondStartState = this.States[stateCount + automaton.Start.Index];

            // todo: make efficient
            bool startIncoming = automaton.Start.HasIncomingTransitions;
            if (!startIncoming || endStates.All(endState => (endState.TransitionCount == 0)))
            {
                foreach (var endState in endStates)
                {
                    for (int transitionIndex = 0; transitionIndex < secondStartState.TransitionCount; transitionIndex++)
                    {
                        var transition = secondStartState.GetTransition(transitionIndex);

                        if (group != 0)
                        {
                            transition.Group = group;
                        }

                        if (transition.DestinationStateIndex == secondStartState.Index)
                        {
                            transition.DestinationStateIndex = endState.Index;
                        }
                        else
                        {
                            transition.Weight = Weight.Product(transition.Weight, endState.EndWeight);
                        }

                        endState.Data.AddTransition(transition);
                    }

                    endState.SetEndWeight(Weight.Product(endState.EndWeight, secondStartState.EndWeight));
                }

                this.States.Remove(secondStartState.Index);
                return;
            }

            for (int i = 0; i < endStates.Count; i++)
            {
                State state = endStates[i];
                state.AddEpsilonTransition(state.EndWeight, secondStartState, group);
                state.SetEndWeight(Weight.Zero);
            }
            */

                /*
                /// <summary>
                /// A version of <see cref="AppendInPlace(TThis, int)"/> that is guaranteed to preserve
                /// the states of both the original automaton and the automaton being appended in the result.
                /// </summary>
                /// <param name="automaton">The automaton to append.</param>
                /// <remarks>
                /// Useful for implementing functions like <see cref="Repeat(TThis, Vector)"/>,
                /// where on-the-fly result optimization creates unnecessary complications.
                /// </remarks>
                private void AppendInPlaceNoOptimizations(TThis automaton)
                {
                    if (ReferenceEquals(automaton, this))
                    {
                        automaton = automaton.Clone();
                    }

                    var stateCount = this.States.Count;
                    var endStates = this.States.Where(nd => nd.CanEnd).ToList();

                    this.States.Append(automaton.States);
                    var secondStartState = this.States[stateCount + automaton.Start.Index];

                    foreach (var state in endStates)
                    {
                        state.AddEpsilonTransition(state.EndWeight, secondStartState);
                        state.SetEndWeight(Weight.Zero);
                    }
                }
                */
            }

            public void SimplifyIfNeeded()
            {
                throw new NotImplementedException();
            }

            public void RemoveDeadStates()
            {
                throw new NotImplementedException();
            }

            internal StateCollection GetStorage()
            {
                throw new NotImplementedException();
            }

            public TThis GetAutomaton()
            {
                throw new NotImplementedException();
            }

            public struct StateBuilder
            {
                private Builder builder;

                public int Index { get; }

                public bool CanEnd => this.builder.states[this.Index].CanEnd;

                public Weight EndWeight => this.builder.states[this.Index].EndWeight;

                public int TransitionCount => throw new NotImplementedException();

                internal StateBuilder(Builder builder, int index)
                {
                    this.builder = builder;
                    this.Index = index;
                }

                public void SetEndWeight(Weight weight)
                {
                    var state = this.builder.states[this.Index];
                    state.EndWeight = weight;
                    this.builder.states[this.Index] = state;
                }

                public StateBuilder AddTransition(Transition transition)
                {
                    var transitionIndex = this.builder.transitions.Count;
                    this.builder.transitions.Add(
                        new LinkedTransition
                        {
                            transition = transition,
                            next = this.builder.states[this.Index].FirstTransition,
                        });
                    var state = this.builder.states[this.Index];
                    state.FirstTransition = transitionIndex;
                    this.builder.states[this.Index] = state;
                    return new StateBuilder(this.builder, transition.DestinationStateIndex);
                }

                /// <summary>
                /// Adds a transition to the current state.
                /// </summary>
                /// <param name="elementDistribution">
                /// The element distribution associated with the transition.
                /// If the value of this parameter is <see langword="null"/>, an epsilon transition will be created.
                /// </param>
                /// <param name="weight">The transition weight.</param>
                /// <param name="destinationStateIndex">
                /// The destination state of the added transition.
                /// If the value of this parameter is <see langword="null"/>, a new state will be created.</param>
                /// <param name="group">The group of the added transition.</param>
                /// <returns>The destination state of the added transition.</returns>
                public StateBuilder AddTransition(
                    Option<TElementDistribution> elementDistribution,
                    Weight weight,
                    int? destinationStateIndex = null,
                    int group = 0)
                {
                    if (destinationStateIndex == null)
                    {
                        destinationStateIndex = this.builder.AddState().Index;
                    }

                    return this.AddTransition(
                        new Transition(elementDistribution, weight, destinationStateIndex.Value, group));
                }

                /// <summary>
                /// Adds a transition labeled with a given element to the current state.
                /// </summary>
                /// <param name="element">The element.</param>
                /// <param name="weight">The transition weight.</param>
                /// <param name="destinationStateIndex">
                /// The destination state of the added transition.
                /// If the value of this parameter is <see langword="null"/>, a new state will be created.</param>
                /// <param name="group">The group of the added transition.</param>
                /// <returns>The destination state of the added transition.</returns>
                public StateBuilder AddTransition(
                    TElement element,
                    Weight weight,
                    int? destinationStateIndex = null,
                    int group = 0)
                {
                    return this.AddTransition(
                        new TElementDistribution {Point = element}, weight, destinationStateIndex, group);
                }

                /// <summary>
                /// Adds an epsilon transition to the current state.
                /// </summary>
                /// <param name="weight">The transition weight.</param>
                /// <param name="destinationStateIndex">
                /// The destination state of the added transition.
                /// If the value of this parameter is <see langword="null"/>, a new state will be created.</param>
                /// <param name="group">The group of the added transition.</param>
                /// <returns>The destination state of the added transition.</returns>
                public StateBuilder AddEpsilonTransition(
                    Weight weight, int? destinationStateIndex = null, int group = 0)
                {
                    return this.AddTransition(Option.None, weight, destinationStateIndex, group);
                }

                /// <summary>
                /// Adds a self-transition labeled with a given element to the current state.
                /// </summary>
                /// <param name="element">The element.</param>
                /// <param name="weight">The transition weight.</param>
                /// <param name="group">The group of the added transition.</param>
                /// <returns>The current state.</returns>
                public StateBuilder AddSelfTransition(TElement element, Weight weight, int group = 0)
                {
                    return this.AddTransition(element, weight, this.Index, group);
                }

                /// <summary>
                /// Adds a self-transition to the current state.
                /// </summary>
                /// <param name="elementDistribution">
                /// The element distribution associated with the transition.
                /// If the value of this parameter is <see langword="null"/>, an epsilon transition will be created.
                /// </param>
                /// <param name="weight">The transition weight.</param>
                /// <param name="group">The group of the added transition.</param>
                /// <returns>The current state.</returns>
                public StateBuilder AddSelfTransition(
                    Option<TElementDistribution> elementDistribution, Weight weight, byte group = 0)
                {
                    return this.AddTransition(elementDistribution, weight, this.Index, group);
                }


                /// <summary>
                /// Adds a series of transitions labeled with the elements of a given sequence to the current state,
                /// as well as the intermediate states. All the added transitions have unit weight.
                /// </summary>
                /// <param name="sequence">The sequence.</param>
                /// <param name="destinationStateIndex">
                /// The last state in the transition series.
                /// If the value of this parameter is <see langword="null"/>, a new state will be created.
                /// </param>
                /// <param name="group">The group of the added transitions.</param>
                /// <returns>The last state in the added transition series.</returns>
                public StateBuilder AddTransitionsForSequence(
                    TSequence sequence,
                    int? destinationStateIndex = null,
                    int group = 0)
                {
                    var currentState = this;
                    using (var enumerator = sequence.GetEnumerator())
                    {
                        var moveNext = enumerator.MoveNext();
                        while (moveNext)
                        {
                            var element = enumerator.Current;
                            moveNext = enumerator.MoveNext();
                            currentState = currentState.AddTransition(
                                element, Weight.One, moveNext ? null : destinationStateIndex, group);
                        }
                    }

                    return currentState;
                }

                public TransitionIterator TransitionIterator =>
                    throw new NotImplementedException();
            }

            public struct TransitionIterator
            {
                public Transition Value
                {
                    get => throw new NotImplementedException();
                    set => throw new NotImplementedException();
                }

                public bool Ok => throw new NotImplementedException();

                public void Next()
                {
                    throw new NotImplementedException();
                }
            }

            private struct LinkedTransition
            {
                public Transition transition;
                public int next;
            }
        }
    }
}