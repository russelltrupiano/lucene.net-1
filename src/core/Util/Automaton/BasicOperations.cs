using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/*
 * dk.brics.automaton
 * 
 * Copyright (c) 2001-2009 Anders Moeller
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 * 
 * this SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * this SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Lucene.Net.Util.Automaton
{



	/// <summary>
	/// Basic automata operations.
	/// 
	/// @lucene.experimental
	/// </summary>
	public sealed class BasicOperations
	{

	  private BasicOperations()
	  {
	  }

	  /// <summary>
	  /// Returns an automaton that accepts the concatenation of the languages of the
	  /// given automata.
	  /// <p>
	  /// Complexity: linear in number of states.
	  /// </summary>
	  public static Automaton Concatenate(Automaton a1, Automaton a2)
	  {
		if (a1.Singleton && a2.Singleton)
		{
			return BasicAutomata.MakeString(a1.Singleton_Renamed + a2.Singleton_Renamed);
		}
		if (IsEmpty(a1) || IsEmpty(a2))
		{
		  return BasicAutomata.MakeEmpty();
		}
		// adding epsilon transitions with the NFA concatenation algorithm
		// in this case always produces a resulting DFA, preventing expensive
		// redundant determinize() calls for this common case.
		bool deterministic = a1.Singleton && a2.Deterministic;
		if (a1 == a2)
		{
		  a1 = a1.CloneExpanded();
		  a2 = a2.CloneExpanded();
		}
		else
		{
		  a1 = a1.CloneExpandedIfRequired();
		  a2 = a2.CloneExpandedIfRequired();
		}
		foreach (State s in a1.AcceptStates)
		{
		  s.Accept_Renamed = false;
		  s.AddEpsilon(a2.Initial);
		}
		a1.Deterministic_Renamed = deterministic;
		//a1.clearHashCode();
		a1.ClearNumberedStates();
		a1.CheckMinimizeAlways();
		return a1;
	  }

	  /// <summary>
	  /// Returns an automaton that accepts the concatenation of the languages of the
	  /// given automata.
	  /// <p>
	  /// Complexity: linear in total number of states.
	  /// </summary>
	  public static Automaton Concatenate(IList<Automaton> l)
	  {
		if (l.Count == 0)
		{
			return BasicAutomata.MakeEmptyString();
		}
		bool all_singleton = true;
		foreach (Automaton a in l)
		{
		  if (!a.Singleton)
		  {
			all_singleton = false;
			break;
		  }
		}
		if (all_singleton)
		{
		  StringBuilder b = new StringBuilder();
		  foreach (Automaton a in l)
		  {
			b.Append(a.Singleton_Renamed);
		  }
		  return BasicAutomata.MakeString(b.ToString());
		}
		else
		{
		  foreach (Automaton a in l)
		  {
			if (BasicOperations.IsEmpty(a))
			{
				return BasicAutomata.MakeEmpty();
			}
		  }
		  Set<int?> ids = new HashSet<int?>();
		  foreach (Automaton a in l)
		  {
			ids.add(System.identityHashCode(a));
		  }
		  bool has_aliases = ids.size() != l.Count;
		  Automaton b = l[0];
		  if (has_aliases)
		  {
			  b = b.CloneExpanded();
		  }
		  else
		  {
			  b = b.CloneExpandedIfRequired();
		  }
		  Set<State> ac = b.AcceptStates;
		  bool first = true;
		  foreach (Automaton a in l)
		  {
			if (first)
			{
				first = false;
			}
			else
			{
			  if (a.EmptyString)
			  {
				  continue;
			  }
			  Automaton aa = a;
			  if (has_aliases)
			  {
				  aa = aa.CloneExpanded();
			  }
			  else
			  {
				  aa = aa.CloneExpandedIfRequired();
			  }
			  Set<State> ns = aa.AcceptStates;
			  foreach (State s in ac)
			  {
				s.Accept_Renamed = false;
				s.AddEpsilon(aa.Initial);
				if (s.Accept_Renamed)
				{
					ns.add(s);
				}
			  }
			  ac = ns;
			}
		  }
		  b.Deterministic_Renamed = false;
		  //b.clearHashCode();
		  b.ClearNumberedStates();
		  b.CheckMinimizeAlways();
		  return b;
		}
	  }

	  /// <summary>
	  /// Returns an automaton that accepts the union of the empty string and the
	  /// language of the given automaton.
	  /// <p>
	  /// Complexity: linear in number of states.
	  /// </summary>
	  public static Automaton Optional(Automaton a)
	  {
		a = a.CloneExpandedIfRequired();
		State s = new State();
		s.AddEpsilon(a.Initial);
		s.Accept_Renamed = true;
		a.Initial = s;
		a.Deterministic_Renamed = false;
		//a.clearHashCode();
		a.ClearNumberedStates();
		a.CheckMinimizeAlways();
		return a;
	  }

	  /// <summary>
	  /// Returns an automaton that accepts the Kleene star (zero or more
	  /// concatenated repetitions) of the language of the given automaton. Never
	  /// modifies the input automaton language.
	  /// <p>
	  /// Complexity: linear in number of states.
	  /// </summary>
	  public static Automaton Repeat(Automaton a)
	  {
		a = a.CloneExpanded();
		State s = new State();
		s.Accept_Renamed = true;
		s.AddEpsilon(a.Initial);
		foreach (State p in a.AcceptStates)
		{
		  p.AddEpsilon(s);
		}
		a.Initial = s;
		a.Deterministic_Renamed = false;
		//a.clearHashCode();
		a.ClearNumberedStates();
		a.CheckMinimizeAlways();
		return a;
	  }

	  /// <summary>
	  /// Returns an automaton that accepts <code>min</code> or more concatenated
	  /// repetitions of the language of the given automaton.
	  /// <p>
	  /// Complexity: linear in number of states and in <code>min</code>.
	  /// </summary>
	  public static Automaton Repeat(Automaton a, int min)
	  {
		if (min == 0)
		{
			return Repeat(a);
		}
		IList<Automaton> @as = new List<Automaton>();
		while (min-- > 0)
		{
		  @as.Add(a);
		}
		@as.Add(Repeat(a));
		return Concatenate(@as);
	  }

	  /// <summary>
	  /// Returns an automaton that accepts between <code>min</code> and
	  /// <code>max</code> (including both) concatenated repetitions of the language
	  /// of the given automaton.
	  /// <p>
	  /// Complexity: linear in number of states and in <code>min</code> and
	  /// <code>max</code>.
	  /// </summary>
	  public static Automaton Repeat(Automaton a, int min, int max)
	  {
		if (min > max)
		{
			return BasicAutomata.MakeEmpty();
		}
		max -= min;
		a.ExpandSingleton();
		Automaton b;
		if (min == 0)
		{
			b = BasicAutomata.MakeEmptyString();
		}
		else if (min == 1)
		{
			b = a.Clone();
		}
		else
		{
		  IList<Automaton> @as = new List<Automaton>();
		  while (min-- > 0)
		  {
			@as.Add(a);
		  }
		  b = Concatenate(@as);
		}
		if (max > 0)
		{
		  Automaton d = a.Clone();
		  while (--max > 0)
		  {
			Automaton c = a.Clone();
			foreach (State p in c.AcceptStates)
			{
			  p.AddEpsilon(d.Initial);
			}
			d = c;
		  }
		  foreach (State p in b.AcceptStates)
		  {
			p.AddEpsilon(d.Initial);
		  }
		  b.Deterministic_Renamed = false;
		  //b.clearHashCode();
		  b.ClearNumberedStates();
		  b.CheckMinimizeAlways();
		}
		return b;
	  }

	  /// <summary>
	  /// Returns a (deterministic) automaton that accepts the complement of the
	  /// language of the given automaton.
	  /// <p>
	  /// Complexity: linear in number of states (if already deterministic).
	  /// </summary>
	  public static Automaton Complement(Automaton a)
	  {
		a = a.CloneExpandedIfRequired();
		a.Determinize();
		a.Totalize();
		foreach (State p in a.NumberedStates)
		{
		  p.Accept_Renamed = !p.Accept_Renamed;
		}
		a.RemoveDeadTransitions();
		return a;
	  }

	  /// <summary>
	  /// Returns a (deterministic) automaton that accepts the intersection of the
	  /// language of <code>a1</code> and the complement of the language of
	  /// <code>a2</code>. As a side-effect, the automata may be determinized, if not
	  /// already deterministic.
	  /// <p>
	  /// Complexity: quadratic in number of states (if already deterministic).
	  /// </summary>
	  public static Automaton Minus(Automaton a1, Automaton a2)
	  {
		if (BasicOperations.IsEmpty(a1) || a1 == a2)
		{
			return BasicAutomata.MakeEmpty();
		}
		if (BasicOperations.IsEmpty(a2))
		{
			return a1.CloneIfRequired();
		}
		if (a1.Singleton)
		{
		  if (BasicOperations.Run(a2, a1.Singleton_Renamed))
		  {
			  return BasicAutomata.MakeEmpty();
		  }
		  else
		  {
			  return a1.CloneIfRequired();
		  }
		}
		return Intersection(a1, a2.Complement());
	  }

	  /// <summary>
	  /// Returns an automaton that accepts the intersection of the languages of the
	  /// given automata. Never modifies the input automata languages.
	  /// <p>
	  /// Complexity: quadratic in number of states.
	  /// </summary>
	  public static Automaton Intersection(Automaton a1, Automaton a2)
	  {
		if (a1.Singleton)
		{
		  if (BasicOperations.Run(a2, a1.Singleton_Renamed))
		  {
			  return a1.CloneIfRequired();
		  }
		  else
		  {
			  return BasicAutomata.MakeEmpty();
		  }
		}
		if (a2.Singleton)
		{
		  if (BasicOperations.Run(a1, a2.Singleton_Renamed))
		  {
			  return a2.CloneIfRequired();
		  }
		  else
		  {
			  return BasicAutomata.MakeEmpty();
		  }
		}
		if (a1 == a2)
		{
			return a1.CloneIfRequired();
		}
		Transition[][] transitions1 = a1.SortedTransitions;
		Transition[][] transitions2 = a2.SortedTransitions;
		Automaton c = new Automaton();
		LinkedList<StatePair> worklist = new LinkedList<StatePair>();
		Dictionary<StatePair, StatePair> newstates = new Dictionary<StatePair, StatePair>();
		StatePair p = new StatePair(c.Initial, a1.Initial, a2.Initial);
		worklist.AddLast(p);
		newstates[p] = p;
		while (worklist.Count > 0)
		{
		  p = worklist.RemoveFirst();
		  p.s.Accept_Renamed = p.S1.Accept_Renamed && p.S2.Accept_Renamed;
		  Transition[] t1 = transitions1[p.S1.number];
		  Transition[] t2 = transitions2[p.S2.number];
		  for (int n1 = 0, b2 = 0; n1 < t1.Length; n1++)
		  {
			while (b2 < t2.Length && t2[b2].Max_Renamed < t1[n1].Min_Renamed)
			{
			  b2++;
			}
			for (int n2 = b2; n2 < t2.Length && t1[n1].Max_Renamed >= t2[n2].Min_Renamed; n2++)
			{
			  if (t2[n2].Max_Renamed >= t1[n1].Min_Renamed)
			  {
				StatePair q = new StatePair(t1[n1].To, t2[n2].To);
				StatePair r = newstates[q];
				if (r == null)
				{
				  q.s = new State();
				  worklist.AddLast(q);
				  newstates[q] = q;
				  r = q;
				}
				int min = t1[n1].Min_Renamed > t2[n2].Min_Renamed ? t1[n1].Min_Renamed : t2[n2].Min_Renamed;
				int max = t1[n1].Max_Renamed < t2[n2].Max_Renamed ? t1[n1].Max_Renamed : t2[n2].Max_Renamed;
				p.s.AddTransition(new Transition(min, max, r.s));
			  }
			}
		  }
		}
		c.Deterministic_Renamed = a1.Deterministic_Renamed && a2.Deterministic_Renamed;
		c.RemoveDeadTransitions();
		c.CheckMinimizeAlways();
		return c;
	  }

	  /// <summary>
	  /// Returns true if these two automata accept exactly the
	  ///  same language.  this is a costly computation!  Note
	  ///  also that a1 and a2 will be determinized as a side
	  ///  effect. 
	  /// </summary>
	  public static bool SameLanguage(Automaton a1, Automaton a2)
	  {
		if (a1 == a2)
		{
		  return true;
		}
		if (a1.Singleton && a2.Singleton)
		{
		  return a1.Singleton_Renamed.Equals(a2.Singleton_Renamed);
		}
		else if (a1.Singleton)
		{
		  // subsetOf is faster if the first automaton is a singleton
		  return SubsetOf(a1, a2) && SubsetOf(a2, a1);
		}
		else
		{
		  return SubsetOf(a2, a1) && SubsetOf(a1, a2);
		}
	  }

	  /// <summary>
	  /// Returns true if the language of <code>a1</code> is a subset of the language
	  /// of <code>a2</code>. As a side-effect, <code>a2</code> is determinized if
	  /// not already marked as deterministic.
	  /// <p>
	  /// Complexity: quadratic in number of states.
	  /// </summary>
	  public static bool SubsetOf(Automaton a1, Automaton a2)
	  {
		if (a1 == a2)
		{
			return true;
		}
		if (a1.Singleton)
		{
		  if (a2.Singleton)
		  {
			  return a1.Singleton_Renamed.Equals(a2.Singleton_Renamed);
		  }
		  return BasicOperations.Run(a2, a1.Singleton_Renamed);
		}
		a2.Determinize();
		Transition[][] transitions1 = a1.SortedTransitions;
		Transition[][] transitions2 = a2.SortedTransitions;
		LinkedList<StatePair> worklist = new LinkedList<StatePair>();
		HashSet<StatePair> visited = new HashSet<StatePair>();
		StatePair p = new StatePair(a1.Initial, a2.Initial);
		worklist.AddLast(p);
		visited.Add(p);
		while (worklist.Count > 0)
		{
		  p = worklist.RemoveFirst();
		  if (p.S1.accept && !p.S2.Accept_Renamed)
		  {
			return false;
		  }
		  Transition[] t1 = transitions1[p.S1.number];
		  Transition[] t2 = transitions2[p.S2.number];
		  for (int n1 = 0, b2 = 0; n1 < t1.Length; n1++)
		  {
			while (b2 < t2.Length && t2[b2].Max_Renamed < t1[n1].Min_Renamed)
			{
			  b2++;
			}
			int min1 = t1[n1].Min_Renamed, max1 = t1[n1].Max_Renamed;

			for (int n2 = b2; n2 < t2.Length && t1[n1].Max_Renamed >= t2[n2].Min_Renamed; n2++)
			{
			  if (t2[n2].Min_Renamed > min1)
			  {
				return false;
			  }
			  if (t2[n2].Max_Renamed < char.MAX_CODE_POINT)
			  {
				  min1 = t2[n2].Max_Renamed + 1;
			  }
			  else
			  {
				min1 = char.MAX_CODE_POINT;
				max1 = char.MIN_CODE_POINT;
			  }
			  StatePair q = new StatePair(t1[n1].To, t2[n2].To);
			  if (!visited.Contains(q))
			  {
				worklist.AddLast(q);
				visited.Add(q);
			  }
			}
			if (min1 <= max1)
			{
			  return false;
			}
		  }
		}
		return true;
	  }

	  /// <summary>
	  /// Returns an automaton that accepts the union of the languages of the given
	  /// automata.
	  /// <p>
	  /// Complexity: linear in number of states.
	  /// </summary>
	  public static Automaton Union(Automaton a1, Automaton a2)
	  {
		if ((a1.Singleton && a2.Singleton && a1.Singleton_Renamed.Equals(a2.Singleton_Renamed)) || a1 == a2)
		{
			return a1.CloneIfRequired();
		}
		if (a1 == a2)
		{
		  a1 = a1.CloneExpanded();
		  a2 = a2.CloneExpanded();
		}
		else
		{
		  a1 = a1.CloneExpandedIfRequired();
		  a2 = a2.CloneExpandedIfRequired();
		}
		State s = new State();
		s.AddEpsilon(a1.Initial);
		s.AddEpsilon(a2.Initial);
		a1.Initial = s;
		a1.Deterministic_Renamed = false;
		//a1.clearHashCode();
		a1.ClearNumberedStates();
		a1.CheckMinimizeAlways();
		return a1;
	  }

	  /// <summary>
	  /// Returns an automaton that accepts the union of the languages of the given
	  /// automata.
	  /// <p>
	  /// Complexity: linear in number of states.
	  /// </summary>
	  public static Automaton Union(ICollection<Automaton> l)
	  {
		Set<int?> ids = new HashSet<int?>();
		foreach (Automaton a in l)
		{
		  ids.add(System.identityHashCode(a));
		}
		bool has_aliases = ids.size() != l.Count;
		State s = new State();
		foreach (Automaton b in l)
		{
		  if (BasicOperations.IsEmpty(b))
		  {
			  continue;
		  }
		  Automaton bb = b;
		  if (has_aliases)
		  {
			  bb = bb.CloneExpanded();
		  }
		  else
		  {
			  bb = bb.CloneExpandedIfRequired();
		  }
		  s.AddEpsilon(bb.Initial);
		}
		Automaton a = new Automaton();
		a.Initial = s;
		a.Deterministic_Renamed = false;
		//a.clearHashCode();
		a.ClearNumberedStates();
		a.CheckMinimizeAlways();
		return a;
	  }

	  // Simple custom ArrayList<Transition>
	  private sealed class TransitionList
	  {
		internal Transition[] Transitions = new Transition[2];
		internal int Count;

		public void Add(Transition t)
		{
		  if (Transitions.Length == Count)
		  {
			Transition[] newArray = new Transition[ArrayUtil.Oversize(1 + Count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			Array.Copy(Transitions, 0, newArray, 0, Count);
			Transitions = newArray;
		  }
		  Transitions[Count++] = t;
		}
	  }

	  // Holds all transitions that start on this int point, or
	  // end at this point-1
	  private sealed class PointTransitions : IComparable<PointTransitions>
	  {
		internal int Point;
		internal readonly TransitionList Ends = new TransitionList();
		internal readonly TransitionList Starts = new TransitionList();
		public int CompareTo(PointTransitions other)
		{
		  return Point - other.Point;
		}

		public void Reset(int point)
		{
		  this.Point = point;
		  Ends.Count = 0;
		  Starts.Count = 0;
		}

		public override bool Equals(object other)
		{
		  return ((PointTransitions) other).Point == Point;
		}

		public override int HashCode()
		{
		  return Point;
		}
	  }

	  private sealed class PointTransitionSet
	  {
		internal int Count;
		internal PointTransitions[] Points = new PointTransitions[5];

		internal const int HASHMAP_CUTOVER = 30;
		internal readonly Dictionary<int?, PointTransitions> Map = new Dictionary<int?, PointTransitions>();
		internal bool UseHash = false;

		internal PointTransitions Next(int point)
		{
		  // 1st time we are seeing this point
		  if (Count == Points.Length)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final PointTransitions[] newArray = new PointTransitions[Lucene.Net.Util.ArrayUtil.oversize(1+count, Lucene.Net.Util.RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			PointTransitions[] newArray = new PointTransitions[ArrayUtil.Oversize(1 + Count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			Array.Copy(Points, 0, newArray, 0, Count);
			Points = newArray;
		  }
		  PointTransitions points0 = Points[Count];
		  if (points0 == null)
		  {
			points0 = Points[Count] = new PointTransitions();
		  }
		  points0.Reset(point);
		  Count++;
		  return points0;
		}

		internal PointTransitions Find(int point)
		{
		  if (UseHash)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer pi = point;
			int? pi = point;
			PointTransitions p = Map[pi];
			if (p == null)
			{
			  p = Next(point);
			  Map[pi] = p;
			}
			return p;
		  }
		  else
		  {
			for (int i = 0;i < Count;i++)
			{
			  if (Points[i].Point == point)
			  {
				return Points[i];
			  }
			}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final PointTransitions p = next(point);
			PointTransitions p = Next(point);
			if (Count == HASHMAP_CUTOVER)
			{
			  // switch to HashMap on the fly
			  Debug.Assert(Map.Count == 0);
			  for (int i = 0;i < Count;i++)
			  {
				Map[Points[i].Point] = Points[i];
			  }
			  UseHash = true;
			}
			return p;
		  }
		}

		public void Reset()
		{
		  if (UseHash)
		  {
			Map.Clear();
			UseHash = false;
		  }
		  Count = 0;
		}

		public void Sort()
		{
		  // Tim sort performs well on already sorted arrays:
		  if (Count > 1)
		  {
			  ArrayUtil.TimSort(Points, 0, Count);
		  }
		}

		public void Add(Transition t)
		{
		  Find(t.Min_Renamed).Starts.add(t);
		  Find(1 + t.Max_Renamed).Ends.add(t);
		}

		public override string ToString()
		{
		  StringBuilder s = new StringBuilder();
		  for (int i = 0;i < Count;i++)
		  {
			if (i > 0)
			{
			  s.Append(' ');
			}
			s.Append(Points[i].Point).Append(':').Append(Points[i].Starts.count).Append(',').Append(Points[i].Ends.Count);
		  }
		  return s.ToString();
		}
	  }

	  /// <summary>
	  /// Determinizes the given automaton.
	  /// <p>
	  /// Worst case complexity: exponential in number of states.
	  /// </summary>
	  public static void Determinize(Automaton a)
	  {
		if (a.Deterministic_Renamed || a.Singleton)
		{
		  return;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State[] allStates = a.getNumberedStates();
		State[] allStates = a.NumberedStates;

		// subset construction
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean initAccept = a.initial.accept;
		bool initAccept = a.Initial.accept;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int initNumber = a.initial.number;
		int initNumber = a.Initial.number;
		a.Initial = new State();
		SortedIntSet.FrozenIntSet initialset = new SortedIntSet.FrozenIntSet(initNumber, a.Initial);

		LinkedList<SortedIntSet.FrozenIntSet> worklist = new LinkedList<SortedIntSet.FrozenIntSet>();
		IDictionary<SortedIntSet.FrozenIntSet, State> newstate = new Dictionary<SortedIntSet.FrozenIntSet, State>();

		worklist.AddLast(initialset);

		a.Initial.accept = initAccept;
		newstate[initialset] = a.Initial;

		int newStateUpto = 0;
		State[] newStatesArray = new State[5];
		newStatesArray[newStateUpto] = a.Initial;
		a.Initial.number = newStateUpto;
		newStateUpto++;

		// like Set<Integer,PointTransitions>
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final PointTransitionSet points = new PointTransitionSet();
		PointTransitionSet points = new PointTransitionSet();

		// like SortedMap<Integer,Integer>
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SortedIntSet statesSet = new SortedIntSet(5);
		SortedIntSet statesSet = new SortedIntSet(5);

		while (worklist.Count > 0)
		{
		  SortedIntSet.FrozenIntSet s = worklist.RemoveFirst();

		  // Collate all outgoing transitions by min/1+max:
		  for (int i = 0;i < s.Values.Length;i++)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State s0 = allStates[s.values[i]];
			State s0 = allStates[s.Values[i]];
			for (int j = 0;j < s0.NumTransitions_Renamed;j++)
			{
			  points.Add(s0.TransitionsArray[j]);
			}
		  }

		  if (points.Count == 0)
		  {
			// No outgoing transitions -- skip it
			continue;
		  }

		  points.Sort();

		  int lastPoint = -1;
		  int accCount = 0;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State r = s.state;
		  State r = s.State;
		  for (int i = 0;i < points.Count;i++)
		  {

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int point = points.points[i].point;
			int point = points.Points[i].point;

			if (statesSet.Upto > 0)
			{
			  Debug.Assert(lastPoint != -1);

			  statesSet.ComputeHash();

			  State q = newstate[statesSet];
			  if (q == null)
			  {
				q = new State();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SortedIntSet.FrozenIntSet p = statesSet.freeze(q);
				SortedIntSet.FrozenIntSet p = statesSet.Freeze(q);
				worklist.AddLast(p);
				if (newStateUpto == newStatesArray.Length)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State[] newArray = new State[Lucene.Net.Util.ArrayUtil.oversize(1+newStateUpto, Lucene.Net.Util.RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
				  State[] newArray = new State[ArrayUtil.Oversize(1 + newStateUpto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
				  Array.Copy(newStatesArray, 0, newArray, 0, newStateUpto);
				  newStatesArray = newArray;
				}
				newStatesArray[newStateUpto] = q;
				q.Number_Renamed = newStateUpto;
				newStateUpto++;
				q.Accept_Renamed = accCount > 0;
				newstate[p] = q;
			  }
			  else
			  {
				assert(accCount > 0 ? true:false) == q.Accept_Renamed: "accCount=" + accCount + " vs existing accept=" + q.Accept_Renamed + " states=" + statesSet;
			  }

			  r.AddTransition(new Transition(lastPoint, point - 1, q));
			}

			// process transitions that end on this point
			// (closes an overlapping interval)
			Transition[] transitions = points.Points[i].ends.transitions;
			int limit = points.Points[i].ends.count;
			for (int j = 0;j < limit;j++)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transition t = transitions[j];
			  Transition t = transitions[j];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer num = t.to.number;
			  int? num = t.To.number;
			  statesSet.Decr(num);
			  accCount -= t.To.accept ? 1:0;
			}
			points.Points[i].ends.count = 0;

			// process transitions that start on this point
			// (opens a new interval)
			transitions = points.Points[i].starts.transitions;
			limit = points.Points[i].starts.count;
			for (int j = 0;j < limit;j++)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transition t = transitions[j];
			  Transition t = transitions[j];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer num = t.to.number;
			  int? num = t.To.number;
			  statesSet.Incr(num);
			  accCount += t.To.accept ? 1:0;
			}
			lastPoint = point;
			points.Points[i].starts.count = 0;
		  }
		  points.Reset();
		  Debug.Assert(statesSet.Upto == 0, "upto=" + statesSet.Upto);
		}
		a.Deterministic_Renamed = true;
		a.SetNumberedStates(newStatesArray, newStateUpto);
	  }

	  /// <summary>
	  /// Adds epsilon transitions to the given automaton. this method adds extra
	  /// character interval transitions that are equivalent to the given set of
	  /// epsilon transitions.
	  /// </summary>
	  /// <param name="pairs"> collection of <seealso cref="StatePair"/> objects representing pairs of
	  ///          source/destination states where epsilon transitions should be
	  ///          added </param>
	  public static void AddEpsilons(Automaton a, ICollection<StatePair> pairs)
	  {
		a.ExpandSingleton();
		Dictionary<State, HashSet<State>> forward = new Dictionary<State, HashSet<State>>();
		Dictionary<State, HashSet<State>> back = new Dictionary<State, HashSet<State>>();
		foreach (StatePair p in pairs)
		{
		  HashSet<State> to = forward[p.S1];
		  if (to == null)
		  {
			to = new HashSet<>();
			forward[p.S1] = to;
		  }
		  to.Add(p.S2);
		  HashSet<State> from = back[p.S2];
		  if (from == null)
		  {
			from = new HashSet<>();
			back[p.S2] = from;
		  }
		  from.Add(p.S1);
		}
		// calculate epsilon closure
		LinkedList<StatePair> worklist = new LinkedList<StatePair>(pairs);
		HashSet<StatePair> workset = new HashSet<StatePair>(pairs);
		while (worklist.Count > 0)
		{
		  StatePair p = worklist.RemoveFirst();
		  workset.Remove(p);
		  HashSet<State> to = forward[p.S2];
		  HashSet<State> from = back[p.S1];
		  if (to != null)
		  {
			foreach (State s in to)
			{
			  StatePair pp = new StatePair(p.S1, s);
			  if (!pairs.Contains(pp))
			  {
				pairs.Add(pp);
				forward[p.S1].Add(s);
				back[s].Add(p.S1);
				worklist.AddLast(pp);
				workset.Add(pp);
				if (from != null)
				{
				  foreach (State q in from)
				  {
					StatePair qq = new StatePair(q, p.S1);
					if (!workset.Contains(qq))
					{
					  worklist.AddLast(qq);
					  workset.Add(qq);
					}
				  }
				}
			  }
			}
		  }
		}
		// add transitions
		foreach (StatePair p in pairs)
		{
		  p.S1.addEpsilon(p.S2);
		}
		a.Deterministic_Renamed = false;
		//a.clearHashCode();
		a.ClearNumberedStates();
		a.CheckMinimizeAlways();
	  }

	  /// <summary>
	  /// Returns true if the given automaton accepts the empty string and nothing
	  /// else.
	  /// </summary>
	  public static bool IsEmptyString(Automaton a)
	  {
		if (a.Singleton)
		{
			return a.Singleton_Renamed.Length == 0;
		}
		else
		{
			return a.Initial.accept && a.Initial.numTransitions() == 0;
		}
	  }

	  /// <summary>
	  /// Returns true if the given automaton accepts no strings.
	  /// </summary>
	  public static bool IsEmpty(Automaton a)
	  {
		if (a.Singleton)
		{
			return false;
		}
		return !a.Initial.accept && a.Initial.numTransitions() == 0;
	  }

	  /// <summary>
	  /// Returns true if the given automaton accepts all strings.
	  /// </summary>
	  public static bool IsTotal(Automaton a)
	  {
		if (a.Singleton)
		{
			return false;
		}
		if (a.Initial.accept && a.Initial.numTransitions() == 1)
		{
		  Transition t = a.Initial.Transitions.GetEnumerator().next();
		  return t.To == a.Initial && t.Min_Renamed == char.MIN_CODE_POINT && t.Max_Renamed == char.MAX_CODE_POINT;
		}
		return false;
	  }

	  /// <summary>
	  /// Returns true if the given string is accepted by the automaton.
	  /// <p>
	  /// Complexity: linear in the length of the string.
	  /// <p>
	  /// <b>Note:</b> for full performance, use the <seealso cref="RunAutomaton"/> class.
	  /// </summary>
	  public static bool Run(Automaton a, string s)
	  {
		if (a.Singleton)
		{
			return s.Equals(a.Singleton_Renamed);
		}
		if (a.Deterministic_Renamed)
		{
		  State p = a.Initial;
		  for (int i = 0, cp = 0; i < s.Length; i += char.charCount(cp))
		  {
			State q = p.Step(cp = s.codePointAt(i));
			if (q == null)
			{
				return false;
			}
			p = q;
		  }
		  return p.Accept_Renamed;
		}
		else
		{
		  State[] states = a.NumberedStates;
		  LinkedList<State> pp = new LinkedList<State>();
		  LinkedList<State> pp_other = new LinkedList<State>();
		  BitArray bb = new BitArray(states.Length);
		  BitArray bb_other = new BitArray(states.Length);
		  pp.AddLast(a.Initial);
		  List<State> dest = new List<State>();
		  bool accept = a.Initial.accept;
		  for (int i = 0, c = 0; i < s.Length; i += char.charCount(c))
		  {
			c = s.codePointAt(i);
			accept = false;
			pp_other.Clear();
			bb_other.SetAll(false);
			foreach (State p in pp)
			{
			  dest.Clear();
			  p.Step(c, dest);
			  foreach (State q in dest)
			  {
				if (q.Accept_Renamed)
				{
					accept = true;
				}
				if (!bb_other.Get(q.Number_Renamed))
				{
				  bb_other.Set(q.Number_Renamed, true);
				  pp_other.AddLast(q);
				}
			  }
			}
			LinkedList<State> tp = pp;
			pp = pp_other;
			pp_other = tp;
			BitArray tb = bb;
			bb = bb_other;
			bb_other = tb;
		  }
		  return accept;
		}
	  }
	}

}