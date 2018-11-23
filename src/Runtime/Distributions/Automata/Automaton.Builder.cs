// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ML.Probabilistic.Distributions.Automata
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            private int numRemovedTransitions = 0;

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
                var result = new Builder();
                result.AddStates(automaton.States);
                result.StartStateIndex = automaton.startStateIndex;
                return result;
            }

            public static Builder ConstantOn(Weight weight, TSequence sequence)
            {
                var result = Builder.Zero();
                result.Start.AddTransitionsForSequence(sequence).SetEndWeight(weight);
                return result;
            }

            public StateBuilder AddState()
            {
                var index = this.states.Count;
                this.states.Add(
                    new StateData
                    {
                        FirstTransition = -1,
                        EndWeight = Weight.Zero,
                    });
                return new StateBuilder(this, index);
            }

            public void AddStates(int count)
            {
                for (var i = 0; i < count; ++i)
                {
                    AddState();
                }
            }

            public void AddStates(StateCollection states)
            {
                var oldStateCount = states.Count;
                foreach (var state in states)
                {
                    var stateBuilder = this.AddState();
                    stateBuilder.SetEndWeight(state.EndWeight);
                    foreach (var transition in state.Transitions)
                    {
                        var updatedTransition = transition;
                        updatedTransition.DestinationStateIndex += oldStateCount;
                        stateBuilder.AddTransition(updatedTransition);
                    }
                }
            }

            public void Append(
                Automaton<TSequence, TElement, TElementDistribution, TSequenceManipulator, TThis> automaton,
                int group = 0,
                bool avoidEpsilonTransitions = true)
            {
                var oldStateCount = this.states.Count;

                foreach (var state in automaton.States)
                {
                    var stateBuilder = this.AddState();
                    stateBuilder.SetEndWeight(state.EndWeight);
                    foreach (var transition in state.Transitions)
                    {
                        var updatedTransition = transition;
                        updatedTransition.DestinationStateIndex += oldStateCount;
                        if (group != 0)
                        {
                            updatedTransition.Group = group;
                        }

                        stateBuilder.AddTransition(updatedTransition);
                    }
                }

                var secondStartState = this[oldStateCount + automaton.startStateIndex];

                if (avoidEpsilonTransitions &&
                    (AllEndStatesHaveNoTransitions() || !automaton.Start.HasIncomingTransitions))
                {
                    // Remove start state of appended automaton and copy all its transitions to previous end states
                    for (var i = 0; i < oldStateCount; ++i)
                    {
                        var endState = this[i];
                        if (!endState.CanEnd)
                        {
                            continue;
                        }

                        for (var iterator = secondStartState.TransitionIterator; iterator.Ok; iterator.Next())
                        {
                            var transition = iterator.Value;

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

                            endState.AddTransition(transition);
                        }

                        endState.SetEndWeight(Weight.Product(endState.EndWeight, secondStartState.EndWeight));
                    }

                    this.RemoveState(secondStartState.Index);
                }
                else
                {
                    // Just connect all end states with start state of appended automaton
                    for (var i = 0; i < this.states.Count; i++)
                    {
                        var state = this[i];
                        state.AddEpsilonTransition(state.EndWeight, secondStartState.Index, group);
                        state.SetEndWeight(Weight.Zero);
                    }
                }

                bool AllEndStatesHaveNoTransitions()
                {
                    for (var i = 0; i < oldStateCount; ++i)
                    {
                        var state = this.states[i];
                        if (state.CanEnd && state.FirstTransition != -1)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            public void RemoveState(int stateIndex)
            {
                // After state is removed, all its transitions will be dead, count them
                var transitionIndex = this.states[stateIndex].FirstTransition;
                while (transitionIndex != -1)
                {
                    ++this.numRemovedTransitions;
                    transitionIndex = this.transitions[transitionIndex].next;
                }

                this.states.RemoveAt(stateIndex);

                for (var i = 0; i < this.states.Count; ++i)
                {
                    var state = this.states[i];
                    var prevIndex = -1;
                    var curIndex = state.FirstTransition;
                    while (curIndex != -1)
                    {
                        var curLinkedTransition = this.transitions[curIndex];
                        if (curLinkedTransition.transition.DestinationStateIndex == stateIndex)
                        {
                            // unlink transition from linked list, but keep it an array
                            ++this.numRemovedTransitions;
                            if (prevIndex == -1)
                            {
                                state.FirstTransition = curLinkedTransition.next;
                                this.states[i] = state;
                            }
                            else
                            {
                                var prevLinkedTransition = this.transitions[prevIndex];
                                prevLinkedTransition.next = curLinkedTransition.next;
                                this.transitions[prevIndex] = prevLinkedTransition;
                            }
                        }
                        else if (curLinkedTransition.transition.DestinationStateIndex > stateIndex)
                        {
                            curLinkedTransition.transition.DestinationStateIndex -= 1;
                            this.transitions[curIndex] = curLinkedTransition;
                        }

                        prevIndex = curIndex;
                        curIndex = curLinkedTransition.next;
                    }
                }
            }

            public void SimplifyIfNeeded()
            {
                throw new NotImplementedException();
            }

            public void RemoveDeadStates()
            {
                throw new NotImplementedException();
            }

            public TThis GetAutomaton()
            {
                var result = new TThis();
                result.stateCollection = this.GetStateCollection(result);
                result.startStateIndex = this.StartStateIndex;
                return result;
            }

            internal StateCollection GetStateCollection(TThis owner)
            {
                if (this.StartStateIndex < 0 || this.StartStateIndex >= this.states.Count)
                {
                    throw new InvalidOperationException(
                        $"Build automaton must have a valid start state. StartStateIndex = {this.StartStateIndex}, states.Count = {this.states.Count}");
                }

                var resultStates = new StateData[this.states.Count];
                var resultTransitions = new Transition[this.transitions.Count - this.numRemovedTransitions];
                var nextResultTransitionIndex = 0;

                for (var i = 0; i < resultStates.Length; ++i)
                {
                    var state = this.states[i];
                    var transitionCount = 0;
                    var transitionIndex = state.FirstTransition;
                    state.FirstTransition = nextResultTransitionIndex;
                    while (transitionIndex != -1)
                    {
                        resultTransitions[nextResultTransitionIndex] = this.transitions[transitionIndex].transition;
                        ++nextResultTransitionIndex;
                        ++transitionCount;
                        transitionIndex = this.transitions[transitionIndex].next;
                    }
                    state.TransitionCount = transitionCount;
                    resultStates[i] = state;
                }

                return new StateCollection(owner, resultStates, resultTransitions);
            }

            public struct StateBuilder
            {
                private Builder builder;

                public int Index { get; }

                public bool CanEnd => this.builder.states[this.Index].CanEnd;

                public Weight EndWeight => this.builder.states[this.Index].EndWeight;

                public bool HasTransitions => this.builder.states[this.Index].FirstTransition != -1;

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
                    new TransitionIterator(this.builder, this.builder.states[this.Index].FirstTransition);
            }

            public struct TransitionIterator
            {
                private readonly Builder builder;
                private int index;

                public TransitionIterator(Builder builder, int index)
                {
                    this.builder = builder;
                    this.index = index;
                }

                public Transition Value
                {
                    get => this.builder.transitions[this.index].transition;
                    set
                    {
                        var linked = this.builder.transitions[this.index];
                        linked.transition = value;
                        this.builder.transitions[this.index] = linked;
                    }
                }

                public bool Ok => this.index != -1;

                public void Next() =>
                    this.index = this.builder.transitions[this.index].next;
            }

            private struct LinkedTransition
            {
                public Transition transition;
                public int next;
            }
        }
    }
}