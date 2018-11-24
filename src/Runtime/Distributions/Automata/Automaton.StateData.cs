// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ML.Probabilistic.Distributions.Automata
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Serialization;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Serialization;
    using Microsoft.ML.Probabilistic.Utilities;

    public abstract partial class Automaton<TSequence, TElement, TElementDistribution, TSequenceManipulator, TThis>
        where TSequence : class, IEnumerable<TElement>
        where TElementDistribution : IDistribution<TElement>, SettableToProduct<TElementDistribution>, SettableToWeightedSumExact<TElementDistribution>, CanGetLogAverageOf<TElementDistribution>, SettableToPartialUniform<TElementDistribution>, new()
        where TSequenceManipulator : ISequenceManipulator<TSequence, TElement>, new()
        where TThis : Automaton<TSequence, TElement, TElementDistribution, TSequenceManipulator, TThis>, new()
    {
        /// <summary>
        /// Represents a state of an automaton that is stored in the Automaton.states. This is an internal representation
        /// of the state. <see cref="State"/> struct should be used in public APIs.
        /// </summary>
        [Serializable]
        [DataContract]
        internal struct StateData
        {
            [DataMember]
            internal int FirstTransition;

            /// <summary>
            /// The number of outgoing transitions from the state.
            /// </summary>
            [DataMember]
            internal int LastTransition;

            [DataMember]
            internal Weight EndWeight;

            /// <summary>
            /// Initializes a new instance of the <see cref="StateData"/> struct.
            /// </summary>
            public StateData(int firstTransition, int lastTransition, Weight endWeight)
            {
                this.FirstTransition = firstTransition;
                this.LastTransition = lastTransition;
                this.EndWeight = endWeight;
            }

            /// <summary>
            /// Gets a value indicating whether the ending weight of this state is greater than zero.
            /// </summary>
            internal bool CanEnd => !this.EndWeight.IsZero;
        }
    }
}
