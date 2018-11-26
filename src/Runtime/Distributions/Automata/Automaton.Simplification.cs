// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ML.Probabilistic.Distributions.Automata
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Core.Collections;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Utilities;

    /// <content>
    /// Contains classes and methods for automata simplification.
    /// </content>
    public abstract partial class Automaton<TSequence, TElement, TElementDistribution, TSequenceManipulator, TThis>
        where TSequence : class, IEnumerable<TElement>
        where TElementDistribution : IDistribution<TElement>, SettableToProduct<TElementDistribution>, SettableToWeightedSumExact<TElementDistribution>, CanGetLogAverageOf<TElementDistribution>, SettableToPartialUniform<TElementDistribution>, new()
        where TSequenceManipulator : ISequenceManipulator<TSequence, TElement>, new()
        where TThis : Automaton<TSequence, TElement, TElementDistribution, TSequenceManipulator, TThis>, new()
    {
        /// <summary>
        /// Attempts to simplify the structure of the automaton, reducing the number of states and transitions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The simplification procedure works as follows:
        /// <list type="number">
        /// <item><description>
        /// If a pair of states has more than one transition between them, the transitions get merged.
        /// </description></item>
        /// <item><description>
        /// A part of the automaton that is a tree is found.
        /// </description></item>
        /// <item><description>
        /// States and transitions that don't belong to the found tree part are simply copied to the result.
        /// </description></item>
        /// <item><description>
        /// The found tree part is rebuild from scratch. The new tree is essentially a trie:
        /// for example, if the original tree has two paths accepting <c>"abc"</c> and one path accepting <c>"ab"</c>,
        /// the resulting tree has a single path accepting both <c>"ab"</c> and <c>"abc"</c>.
        /// </description></item>
        /// </list>
        /// </para>
        /// <para>The simplification procedure doesn't support automata with non-trivial loops.</para>
        /// </remarks>
        public void Simplify()
        {
            var builder = Builder.FromAutomaton(this);
            var simplification = new Simplification(builder, this.PruneTransitionsWithLogWeightLessThan);
            if (simplification.Simplify())
            {
                this.SwapWith(builder.GetAutomaton());
            }
        }

        public void RemoveDeadStates()
        {
            // TODO: avoid builder construction if there are no dead states
            var builder = Builder.FromAutomaton(this);
            var simplification = new Simplification(builder, this.PruneTransitionsWithLogWeightLessThan);
            if (simplification.RemoveDeadStates())
            {
                this.SwapWith(builder.GetAutomaton());
            }
        }

        /// <summary>
        /// TODO Groups together helper classes used for automata simplification.
        /// </summary>
        public struct Simplification
        {
            private Builder builder;

            private double? pruneTransitionsWithLogWeightLessThan;

            public Simplification(Builder builder, double? pruneTransitionsWithLogWeightLessThan)
            {
                this.builder = builder;
                this.pruneTransitionsWithLogWeightLessThan = pruneTransitionsWithLogWeightLessThan;
            }

            public bool SimplifyIfNeeded()
            {
                if (this.builder.StatesCount > MaxStateCountBeforeSimplification ||
                    this.pruneTransitionsWithLogWeightLessThan != null)
                {
                    return this.Simplify();
                }

                return false;
            }

            public bool Simplify()
            {
                /*
                this.MergeParallelTransitions();

                // TODO FIX FIX FIX
                if (this.builder.GetAutomaton().HasNonTrivialLoops())
                {
                    return false; // TODO: make this stuff work with non-trivial loops
                }

                ArrayDictionary<bool> stateLabels = this.LabelStatesForSimplification();
                var sequenceToLogWeight = this.BuildAcceptedSequenceList(stateLabels);

                TThis result = this.RemoveSimplifiable(stateLabels);
                int firstNonCopiedStateIndex = result.States.Count;

                // Before we rebuild the tree part, we prune out the low probability sequences
                if (this.pruneTransitionsWithLogWeightLessThan != null)
                {
                    // TODO: can we live without thie GetAutomaton() call?
                    double logNorm = this.builder.GetAutomaton().GetLogNormalizer();
                    if (!double.IsInfinity(logNorm))
                    {
                        sequenceToLogWeight = sequenceToLogWeight
                            .Where(s => s.Weight.LogValue - logNorm >= this.pruneTransitionsWithLogWeightLessThan.Value)
                            .ToList();
                    }
                }

                foreach (var weightedSequence in sequenceToLogWeight)
                {
                    this.AddGeneralizedSequence(firstNonCopiedStateIndex, weightedSequence.Sequence, weightedSequence.Weight);
                }
                */

                return true;
            }

            /// <summary>
            /// Optimizes the automaton by removing all states unreachable from the end state.
            /// </summary>
            public bool RemoveDeadStates()
            {
                return this.builder.RemoveStates(FindDeadStates()) > 0;

                bool[] FindDeadStates()
                {
                    throw new NotImplementedException();
                }
            }

            /// <summary>
            /// Removes transitions in the automaton whose log weight is less than the specified threshold.
            /// </summary>
            /// <remarks>
            /// Any states which are unreachable in the resulting automaton are also removed.
            /// </remarks>
            /// <param name="logWeightThreshold">The smallest log weight that a transition can have and not be removed.</param>
            public void RemoveTransitionsWithSmallWeights(double logWeightThreshold)
            {
                for (var i = 0; i < this.builder.StatesCount; ++i)
                {
                    var state = this.builder[i];
                    for (var iterator = state.TransitionIterator; iterator.Ok; iterator.Next())
                    {
                        if (iterator.Value.Weight.LogValue < logWeightThreshold)
                        {
                            iterator.MarkRemoved();
                        }
                    }
                }

                this.builder.RemoveStates(FindOrphanStates());

                bool[] FindOrphanStates()
                {
                    throw new NotImplementedException();
                }
            }
            
            /// <summary>
            /// Labels each state with a value indicating whether the automaton having that state as the start state is a
            /// generalized tree (i.e. a tree with self-loops), which is also unreachable from previously traversed states.
            /// </summary>
            /// <returns>A dictionary mapping state indices to the computed labels.</returns>
            private ArrayDictionary<bool> LabelStatesForSimplification()
            {
                throw new NotImplementedException();
                /*
                var result = new ArrayDictionary<bool>();
                this.DoLabelStatesForSimplification(this.Start, result);
                return result;
                */
            }

            /// <summary>
            /// Recursively labels each state with a value indicating whether the automaton having that state as the start state
            /// is a generalized tree (i.e. a tree with self-loops), which is also unreachable from previously traversed states.
            /// </summary>
            /// <param name="currentState">The currently traversed state.</param>
            /// <param name="stateLabels">A dictionary mapping state indices to the computed labels.</param>
            /// <returns>
            /// <see langword="true"/> if the automaton having <paramref name="currentState"/> having that state as the start state
            /// is a generalized tree and it was the first visit to it, <see langword="false"/> otherwise.
            /// </returns>
            private bool DoLabelStatesForSimplification(State currentState, ArrayDictionary<bool> stateLabels)
            {
                throw new NotImplementedException();

                /*

                if (stateLabels.ContainsKey(currentState.Index))
                {
                    // This is not the first visit to the state
                    return false;
                }

                stateLabels.Add(currentState.Index, true);

                bool isGeneralizedTree = true;
                foreach (var transition in currentState.Transitions)
                {

                    // Self-loops are allowed
                    if (transition.DestinationStateIndex != currentState.Index)
                    {
                        isGeneralizedTree &= this.DoLabelStatesForSimplification(this.States[transition.DestinationStateIndex], stateLabels);
                    }
                }

                // It was the first visit to the state
                stateLabels[currentState.Index] = isGeneralizedTree;
                return isGeneralizedTree;

                */
            }

            /// <summary>
            /// Merges outgoing transitions with the same destination state.
            /// </summary>
            public void MergeParallelTransitions()
            {
                throw new NotImplementedException();

                /*

                for (int stateIndex = 0; stateIndex < this.States.Count; ++stateIndex)
                {
                    var state = this.States[stateIndex];
                    var transitions = state.Transitions;
                    for (int transitionIndex1 = 0; transitionIndex1 < transitions.Count; ++transitionIndex1)
                    {
                        Transition transition1 = transitions[transitionIndex1];
                        for (int transitionIndex2 = transitionIndex1 + 1; transitionIndex2 < transitions.Count; ++transitionIndex2)
                        {
                            Transition transition2 = transitions[transitionIndex2];
                            if (transition1.DestinationStateIndex == transition2.DestinationStateIndex && transition1.Group == transition2.Group)
                            {
                                bool removeTransition2 = false;
                                if (transition1.IsEpsilon && transition2.IsEpsilon)
                                {
                                    transition1.Weight = Weight.Sum(transition1.Weight, transition2.Weight);
                                    state.SetTransition(transitionIndex1, transition1);
                                    removeTransition2 = true;
                                }
                                else if (!transition1.IsEpsilon && !transition2.IsEpsilon)
                                {
                                    var newElementDistribution = new TElementDistribution();
                                    if (double.IsInfinity(transition1.Weight.Value) && double.IsInfinity(transition2.Weight.Value))
                                    {
                                        newElementDistribution.SetToSum(1.0, transition1.ElementDistribution.Value, 1.0, transition2.ElementDistribution.Value);
                                    }
                                    else
                                    {
                                        newElementDistribution.SetToSum(transition1.Weight.Value, transition1.ElementDistribution.Value, transition2.Weight.Value, transition2.ElementDistribution.Value);
                                    }

                                    transition1.ElementDistribution = newElementDistribution;
                                    transition1.Weight = Weight.Sum(transition1.Weight, transition2.Weight);
                                    state.SetTransition(transitionIndex1, transition1);
                                    removeTransition2 = true;
                                }

                                if (removeTransition2)
                                {
                                    state.RemoveTransition(transitionIndex2);
                                    --transitionIndex2;
                                }
                            }
                        }
                    }
                }

                */
            }

            /// <summary>
            /// Creates a copy of the non-simplifiable part of the automaton (states labeled with
            /// <see langword="false"/> by <see cref="LabelStatesForSimplification"/> and their children).
            /// </summary>
            /// <param name="stateLabels">The state labels obtained from <see cref="LabelStatesForSimplification"/>.</param>
            /// <returns>The copied part of the automaton.</returns>
            private TThis RemoveSimplifiable(ArrayDictionary<bool> stateLabels)
            {
                throw new NotImplementedException();

                /*

                var result = Builder.Zero();
                if (stateLabels[this.Start.Index])
                {
                    return result.GetAutomaton();
                }

                var copiedStateCache = new ArrayDictionary<int>();
                result.StartStateIndex = DoCopyNonSimplifiable(this.Start, true);

                return result.GetAutomaton();

                // Recursively creates a copy of the non-simplifiable part of the automaton
                int DoCopyNonSimplifiable(State stateToCopy, bool lookAtLabels)
                {
                    Debug.Assert(!lookAtLabels || !stateLabels[stateToCopy.Index], "States that are not supposed to be copied should not be visited.");

                    if (copiedStateCache.TryGetValue(stateToCopy.Index, out var copiedStateIndex))
                    {
                        return copiedStateIndex;
                    }

                    var copiedState = result.AddState();
                    copiedState.SetEndWeight(stateToCopy.EndWeight);
                    copiedStateCache.Add(stateToCopy.Index, copiedState.Index);

                    foreach (var transitionToCopy in stateToCopy.Transitions)
                    {
                        State destStateToCopy = stateToCopy.Owner.States[transitionToCopy.DestinationStateIndex];
                        if (!lookAtLabels || !stateLabels[destStateToCopy.Index])
                        {
                            int copiedDestStateIndex = DoCopyNonSimplifiable(destStateToCopy, false);
                            copiedState.AddTransition(
                                transitionToCopy.ElementDistribution,
                                transitionToCopy.Weight,
                                copiedDestStateIndex,
                                transitionToCopy.Group);
                        }
                    }

                    return copiedState.Index;
                }

                */
            }

            /// <summary>
            /// Builds a complete list of generalized sequences accepted by the simplifiable part of the automaton.
            /// </summary>
            /// <param name="stateLabels">The state labels obtained from <see cref="LabelStatesForSimplification"/>.</param>
            /// <returns>The list of generalized sequences accepted by the simplifiable part of the automaton.</returns>
            private List<Simplification.WeightedSequence> BuildAcceptedSequenceList(ArrayDictionary<bool> stateLabels)
            {
                throw new NotImplementedException();
                /*
                var sequenceToWeight = new List<Simplification.WeightedSequence>();
                this.DoBuildAcceptedSequenceList(this.Start, stateLabels, sequenceToWeight, new List<Simplification.GeneralizedElement>(), Weight.One);
                return sequenceToWeight;
                */
            }


            private class StackItem
            {
            }

            private class ElementItem : StackItem
            {
                public readonly Simplification.GeneralizedElement? Element;

                public ElementItem(Simplification.GeneralizedElement? element)
                {
                    this.Element = element;
                }
                public override string ToString()
                {
                    return Element.ToString();
                }
            }

            private class StateWeight : StackItem
            {
                public StateWeight(State state, Weight weight)
                {
                    this.State = state;
                    this.Weight = weight;
                }

                public readonly State State;
                public readonly Weight Weight;

                public override string ToString()
                {
                    return $"State: {State}, Weight: {Weight}";
                }
            }

            /// <summary>
            /// Recursively builds a complete list of generalized sequences accepted by the simplifiable part of the automaton.
            /// </summary>
            /// <param name="state">The currently traversed state.</param>
            /// <param name="stateLabels">The state labels obtained from <see cref="LabelStatesForSimplification"/>.</param>
            /// <param name="weightedSequences">The sequence list being built.</param>
            /// <param name="currentSequenceElements">The list of elements of the sequence currently being built.</param>
            /// <param name="currentWeight">The weight of the sequence currently being built.</param>
            private void DoBuildAcceptedSequenceList(
                State state,
                ArrayDictionary<bool> stateLabels,
                List<Simplification.WeightedSequence> weightedSequences,
                List<Simplification.GeneralizedElement> currentSequenceElements,
                Weight currentWeight)
            {
                throw new NotImplementedException();

                /*

                var stack = new Stack<StackItem>();
                stack.Push(new StateWeight(state, currentWeight));

                while (stack.Count > 0)
                {
                    var stackItem = stack.Pop();
                    var elementItem = stackItem as ElementItem;

                    if (elementItem != null)
                    {
                        if (elementItem.Element != null)
                            currentSequenceElements.Add(elementItem.Element.Value);
                        else
                            currentSequenceElements.RemoveAt(currentSequenceElements.Count - 1);
                        continue;
                    }

                    var stateAndWeight = stackItem as StateWeight;

                    state = stateAndWeight.State;
                    currentWeight = stateAndWeight.Weight;

                    // Find a non-epsilon self-loop if there is one
                    Transition? selfLoop = null;
                    foreach (var transition in state.Transitions)
                    {
                        if (transition.DestinationStateIndex != state.Index)
                        {
                            continue;
                        }

                        if (selfLoop == null)
                        {
                            selfLoop = transition;
                        }
                        else
                        {
                            Debug.Fail("Multiple self-loops should have been merged by MergeParallelTransitions()");
                        }
                    }

                    // Push the found self-loop to the end of the current sequence
                    if (selfLoop != null)
                    {
                        currentSequenceElements.Add(new Simplification.GeneralizedElement(
                            selfLoop.Value.ElementDistribution, selfLoop.Value.Group, selfLoop.Value.Weight));
                        stack.Push(new ElementItem(null));
                    }

                    // Can this state produce a sequence?
                    if (state.CanEnd && stateLabels[state.Index])
                    {
                        var sequence = new Simplification.GeneralizedSequence(currentSequenceElements);
                        // TODO: use immutable data structure instead of copying sequences
                        weightedSequences.Add(new Simplification.WeightedSequence(sequence, Weight.Product(currentWeight, state.EndWeight)));
                    }

                    // Traverse the outgoing transitions
                    foreach (var transition in state.Transitions)
                    {
                        // Skip self-loops & disallowed states
                        if (transition.DestinationStateIndex == state.Index ||
                            !stateLabels[transition.DestinationStateIndex])
                        {
                            continue;
                        }

                        if (!transition.IsEpsilon)
                        {
                            // Non-epsilon transitions contribute to the sequence
                            stack.Push(new ElementItem(null));
                        }

                        stack.Push(
                            new StateWeight(
                                States[transition.DestinationStateIndex],
                                Weight.Product(currentWeight, transition.Weight)));

                        if (!transition.IsEpsilon)
                        {
                            stack.Push(
                                new ElementItem(new Simplification.GeneralizedElement(transition.ElementDistribution,
                                    transition.Group, null)));
                        }
                    }
                }

                */
            }

            /// <summary>
            /// Increases the value of this automaton on <paramref name="sequence"/> by <paramref name="weight"/>.
            /// </summary>
            /// <param name="firstAllowedStateIndex">The minimum index of an existing state that can be used for the sequence.</param>
            /// <param name="sequence">The generalized sequence.</param>
            /// <param name="weight">The weight of the sequence.</param>
            /// <remarks>
            /// This function attempts to add as few new states and transitions as possible.
            /// Its implementation is conceptually similar to adding string to a trie.
            /// </remarks>
            private void AddGeneralizedSequence(int firstAllowedStateIndex, Simplification.GeneralizedSequence sequence, Weight weight)
            {
                // TODO
                /*
                // First, try to add at start state
                bool isFreshStartState = this.IsCanonicZero();
                if (this.DoAddGeneralizedSequence(this.Start, isFreshStartState, false, firstAllowedStateIndex, 0, sequence, weight))
                {
                    return;
                }

                // Branch the start state
                State oldStart = this.Start;
                this.Start = this.AddState();
                State otherBranch = this.AddState();
                this.Start.AddEpsilonTransition(Weight.One, oldStart);
                this.Start.AddEpsilonTransition(Weight.One, otherBranch);

                // This should always work
                bool success = this.DoAddGeneralizedSequence(otherBranch, true, false, firstAllowedStateIndex, 0, sequence, weight);
                Debug.Assert(success, "This call must always succeed.");
                */
            }

            /// <summary>
            /// Recursively increases the value of this automaton on <paramref name="sequence"/> by <paramref name="weight"/>.
            /// </summary>
            /// <param name="state">The currently traversed state.</param>
            /// <param name="isNewState">Indicates whether <paramref name="state"/> was just created.</param>
            /// <param name="selfLoopAlreadyMatched">Indicates whether self-loop on <paramref name="state"/> was just matched.</param>
            /// <param name="firstAllowedStateIndex">The minimum index of an existing state that can be used for the sequence.</param>
            /// <param name="currentSequencePos">The current position in the generalized sequence.</param>
            /// <param name="sequence">The generalized sequence.</param>
            /// <param name="weight">The weight of the sequence.</param>
            /// <returns>
            /// <see langword="true"/> if the subsequence starting at <paramref name="currentSequencePos"/> has been successfully merged in,
            /// <see langword="false"/> otherwise.
            /// </returns>
            /// <remarks>
            /// This function attempts to add as few new states and transitions as possible.
            /// Its implementation is conceptually similar to adding string to a trie.
            /// </remarks>
            private bool DoAddGeneralizedSequence(
                State state,
                bool isNewState,
                bool selfLoopAlreadyMatched,
                int firstAllowedStateIndex,
                int currentSequencePos,
                Simplification.GeneralizedSequence sequence,
                Weight weight)
            {
                throw new NotImplementedException();

                /*

                bool success;

                if (currentSequencePos == sequence.Count)
                {
                    if (!selfLoopAlreadyMatched)
                    {
                        // We can't finish in a state with a self-loop
                        foreach (var transition in state.Transitions)
                        {
                            if (transition.DestinationStateIndex == state.Index)
                            {
                                return false;
                            }
                        }
                    }

                    state.SetEndWeight(Weight.Sum(state.EndWeight, weight));
                    return true;
                }

                Simplification.GeneralizedElement element = sequence[currentSequencePos];

                // Treat self-loops elements separately
                if (element.LoopWeight.HasValue)
                {
                    if (selfLoopAlreadyMatched)
                    {
                        // Previous element was also a self-loop, we should try to find an espilon transition
                        for (int i = 0; i < state.TransitionCount; ++i)
                        {
                            Transition transition = state.GetTransition(i);
                            if (transition.DestinationStateIndex != state.Index && transition.IsEpsilon && transition.DestinationStateIndex >= firstAllowedStateIndex)
                            {
                                if (this.DoAddGeneralizedSequence(
                                    this.States[transition.DestinationStateIndex],
                                    false,
                                    false,
                                    firstAllowedStateIndex,
                                    currentSequencePos,
                                    sequence,
                                    Weight.Product(weight, Weight.Inverse(transition.Weight))))
                                {
                                    return true;
                                }
                            }
                        }

                        // Epsilon transition not found, let's create a new one
                        State destination = state.AddEpsilonTransition(Weight.One);
                        success = this.DoAddGeneralizedSequence(destination, true, false, firstAllowedStateIndex, currentSequencePos, sequence, weight);
                        Debug.Assert(success, "This call must always succeed.");
                        return true;
                    }

                    // Find a matching self-loop
                    for (int i = 0; i < state.TransitionCount; ++i)
                    {
                        Transition transition = state.GetTransition(i);

                        if (transition.IsEpsilon && transition.DestinationStateIndex != state.Index && transition.DestinationStateIndex >= firstAllowedStateIndex)
                        {
                            // Try this epsilon transition
                            if (this.DoAddGeneralizedSequence(
                                this.States[transition.DestinationStateIndex], false, false, firstAllowedStateIndex, currentSequencePos, sequence, weight))
                            {
                                return true;
                            }
                        }

                        // Is it a self-loop?
                        if (transition.DestinationStateIndex == state.Index)
                        {
                            // Do self-loops match?
                            if ((transition.Weight == element.LoopWeight.Value) &&
                                (element.Group == transition.Group) &&
                                ((transition.IsEpsilon && element.IsEpsilonSelfLoop) || (!transition.IsEpsilon && !element.IsEpsilonSelfLoop && transition.ElementDistribution.Equals(element.ElementDistribution))))
                            {
                                // Skip the element in the sequence, remain in the same state
                                success = this.DoAddGeneralizedSequence(state, false, true, firstAllowedStateIndex, currentSequencePos + 1, sequence, weight);
                                Debug.Assert(success, "This call must always succeed.");
                                return true;
                            }

                            // State also has a self-loop, but the two doesn't match
                            return false;
                        }
                    }

                    if (!isNewState)
                    {
                        // Can't add self-loop to an existing state, it will change the language accepted by the state
                        return false;
                    }

                    // Add a new self-loop
                    state.AddTransition(element.ElementDistribution, element.LoopWeight.Value, state, element.Group);
                    success = this.DoAddGeneralizedSequence(state, false, true, firstAllowedStateIndex, currentSequencePos + 1, sequence, weight);
                    Debug.Assert(success, "This call must always succeed.");
                    return true;
                }

                // Try to find a transition for the element
                for (int i = 0; i < state.TransitionCount; ++i)
                {
                    Transition transition = state.GetTransition(i);

                    if (transition.IsEpsilon && transition.DestinationStateIndex != state.Index && transition.DestinationStateIndex >= firstAllowedStateIndex)
                    {
                        // Try this epsilon transition
                        if (this.DoAddGeneralizedSequence(
                            this.States[transition.DestinationStateIndex], false, false, firstAllowedStateIndex, currentSequencePos, sequence, weight))
                        {
                            return true;
                        }
                    }

                    // Is it a self-loop?
                    if (transition.DestinationStateIndex == state.Index)
                    {
                        if (selfLoopAlreadyMatched)
                        {
                            // The self-loop was checked or added by the caller
                            continue;
                        }

                        // Can't go through an existing self-loop, it will allow undesired sequences to be accepted
                        return false;
                    }

                    if (transition.DestinationStateIndex < firstAllowedStateIndex ||
                        element.Group != transition.Group ||
                        !element.ElementDistribution.Equals(transition.ElementDistribution))
                    {
                        continue;
                    }

                    // Skip the element in the sequence, move to the destination state
                    // Weight of the existing transition must be taken into account
                    // This case can fail if the next element is a self-loop and the destination state already has a different one
                    if (this.DoAddGeneralizedSequence(
                        this.States[transition.DestinationStateIndex],
                        false,
                        false,
                        firstAllowedStateIndex,
                        currentSequencePos + 1,
                        sequence,
                        Weight.Product(weight, Weight.Inverse(transition.Weight))))
                    {
                        return true;
                    }
                }

                // Add a new transition
                State newChild = state.AddTransition(element.ElementDistribution, Weight.One, default(State), element.Group);
                success = this.DoAddGeneralizedSequence(newChild, true, false, firstAllowedStateIndex, currentSequencePos + 1, sequence, weight);
                Debug.Assert(success, "This call must always succeed.");
                return true;

    */
            }

            /// <summary>
            /// Represents an element of a generalized sequence,
            /// i.e. a distribution over a single symbol or a weighted self-loop.
            /// </summary>
            public struct GeneralizedElement
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="GeneralizedElement"/> struct.
                /// </summary>
                /// <param name="elementDistribution">The element distribution associated with the generalized element.</param>
                /// <param name="group">The group associated with the generalized element.</param>
                /// <param name="loopWeight">
                /// The loop weight associated with the generalized element, <see langword="null"/> if the element does not represent a self-loop.
                /// </param>
                public GeneralizedElement(Option<TElementDistribution> elementDistribution, int group, Weight? loopWeight)
                    : this()
                {
                    Debug.Assert(
                        elementDistribution.HasValue || loopWeight.HasValue,
                        "Epsilon elements are only allowed in combination with self-loops.");

                    this.ElementDistribution = elementDistribution;
                    this.Group = group;
                    this.LoopWeight = loopWeight;
                }

                /// <summary>
                /// Gets the element distribution associated with the generalized element.
                /// </summary>
                public Option<TElementDistribution> ElementDistribution { get; }

                /// <summary>
                /// Gets a value indicating whether this element corresponds to an epsilon self-loop.
                /// </summary>
                public bool IsEpsilonSelfLoop => !this.ElementDistribution.HasValue && this.LoopWeight.HasValue;

                /// <summary>
                /// Gets the group associated with the generalized element.
                /// </summary>
                public int Group { get; private set; }

                /// <summary>
                /// Gets the loop weight associated with the generalized element,
                /// <see langword="null"/> if the element does not represent a self-loop.
                /// </summary>
                public Weight? LoopWeight { get; private set; }

                /// <summary>
                /// Gets the string representation of the generalized element.
                /// </summary>
                /// <returns>The string representation of the generalized element.</returns>
                public override string ToString()
                {
                    string elementDistributionAsString = this.ElementDistribution.Value.IsPointMass ? this.ElementDistribution.Value.Point.ToString() : this.ElementDistribution.ToString();
                    string groupString = this.Group == 0 ? string.Empty : string.Format("#{0}", this.Group);
                    if (this.LoopWeight.HasValue)
                    {
                        return string.Format("{0}{1}*({2})", groupString, elementDistributionAsString, this.LoopWeight.Value);
                    }

                    return string.Format("{0}{1}", groupString, elementDistributionAsString);
                }
            }

            /// <summary>
            /// Represents a sequence of generalized elements.
            /// </summary>
            public class GeneralizedSequence
            {
                /// <summary>
                /// The sequence elements.
                /// </summary>
                private readonly List<GeneralizedElement> elements;

                /// <summary>
                /// Initializes a new instance of the <see cref="GeneralizedSequence"/> class.
                /// </summary>
                /// <param name="elements">The sequence elements.</param>
                public GeneralizedSequence(IEnumerable<GeneralizedElement> elements)
                {
                    this.elements = new List<GeneralizedElement>(elements);
                }

                /// <summary>
                /// Gets the number of elements in the sequence.
                /// </summary>
                public int Count => this.elements.Count;

                /// <summary>
                /// Gets the sequence element with the specified index.
                /// </summary>
                /// <param name="index">The element index.</param>
                /// <returns>The element at the given index.</returns>
                public GeneralizedElement this[int index] => this.elements[index];

                /// <summary>
                /// Gets the string representation of the sequence.
                /// </summary>
                /// <returns>The string representation of the sequence.</returns>
                public override string ToString()
                {
                    var stringBuilder = new StringBuilder();
                    foreach (GeneralizedElement element in this.elements)
                    {
                        stringBuilder.Append(element);
                    }

                    return stringBuilder.ToString();
                }
            }

            public struct WeightedSequence
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="WeightedSequence"/> struct.
                /// </summary>
                /// <param name="sequence">The <see cref="GeneralizedSequence"/></param>
                /// <param name="weight">The <see cref="Weight"/> for the specified sequence</param>
                public WeightedSequence(GeneralizedSequence sequence, Weight weight)
                    : this()
                {
                    this.Sequence = sequence;
                    this.Weight = weight;
                }

                /// <summary>
                /// Gets or sets the <see cref="GeneralizedSequence"/>.
                /// </summary>
                public readonly GeneralizedSequence Sequence;

                /// <summary>
                /// Gets or sets the predicted probability.
                /// </summary>
                public readonly Weight Weight;

                /// <summary>
                /// Gets the string representation of this <see cref="WeightedSequence"/>.
                /// </summary>
                /// <returns>The string representation of the <see cref="WeightedSequence"/>.</returns>
                public override string ToString()
                {
                    return $"[{this.Sequence},{this.Weight}]";
                }

                /// <summary>
                /// Checks if this object is equal to <paramref name="obj"/>.
                /// </summary>
                /// <param name="obj">The object to compare this object with.</param>
                /// <returns>
                /// <see langword="true"/> if this object is equal to <paramref name="obj"/>,
                /// <see langword="false"/> otherwise.
                /// </returns>
                public override bool Equals(object obj)
                {
                    if (obj is WeightedSequence weightedSequence)
                    {
                        return object.Equals(this.Sequence, weightedSequence.Sequence) && object.Equals(this.Weight, weightedSequence.Weight);
                    }

                    return false;
                }

                /// <summary>
                /// Computes the hash code of this object.
                /// </summary>
                /// <returns>The computed hash code.</returns>
                public override int GetHashCode()
                {
                    int result = Hash.Start;

                    result = Hash.Combine(result, this.Sequence.GetHashCode());
                    result = Hash.Combine(result, this.Weight.GetHashCode());

                    return result;
                }
            }
        }
    }
}
