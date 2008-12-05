#region License

/* Copyright (c) 2006 Leslie Sanford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

#endregion

#region Contact

/*
 * Leslie Sanford
 * Email: jabberdabber@hotmail.com
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sanford.StateMachineToolkit
{
	/// <summary>
	/// Represents the base class for all state machines.
	/// You do not derive your state machine classes from this class but rather from one 
	/// of its derived classes, either the <see cref="ActiveStateMachine{TState,TEvent}"/> 
	/// class or the <see cref="PassiveStateMachine{TState,TEvent}"/> class.
	/// </summary>
	public abstract partial class StateMachine<TState, TEvent>
		where TState : struct, IComparable, IFormattable /*, IConvertible*/
		where TEvent : struct, IComparable, IFormattable /*, IConvertible*/
	{
		#region StateMachine Members

		#region Fields

		// The return value of the last action.
		[ThreadStatic]
		private static StateMachine<TState, TEvent> s_currentStateMachine;

		private object m_actionResult;

		// Indicates whether the state machine has been initialized.
		private bool m_initialized;

		private readonly StateMap m_states = new StateMap();
		/// <summary>
		/// The current state.
		/// </summary>
		protected State m_currentState;

		/// <summary>
		/// The current event context.
		/// </summary>
		protected EventContext m_currentEventContext;

		#endregion

		#region Events

		/// <summary>
		/// Occurs before a dispatch starts.
		/// </summary>
		public event EventHandler<TransitionEventArgs<TState, TEvent>> BeginDispatch;
		/// <summary>
		/// Occurs before a transition starts.
		/// </summary>
		public event EventHandler<TransitionEventArgs<TState, TEvent>> BeginTransition;
		/// <summary>
		/// Occurs after a transition is completed.
		/// </summary>
		public event EventHandler<TransitionCompletedEventArgs<TState, TEvent>> TransitionCompleted;
		/// <summary>
		/// Occurs when a transition is declined.
		/// </summary>
		public event EventHandler<TransitionEventArgs<TState, TEvent>> TransitionDeclined;
		/// <summary>
		/// Occurs when an exception is thrown.
		/// </summary>
		public virtual event EventHandler<TransitionErrorEventArgs<TState, TEvent>> ExceptionThrown;

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the StateMachine's initial state.
		/// </summary>
		/// <param name="initialState">
		/// The state that will initially receive events from the StateMachine.
		/// </param>
		protected abstract void Initialize(State initialState);

		/// <summary>
		/// Initializes the StateMachine's initial state.
		/// </summary>
		/// <param name="initialStateID">
		/// The state that will initially receive events from the StateMachine.
		/// </param>
		protected void Initialize(TState initialStateID)
		{
			Initialize(States[initialStateID]);
		}

		/// <summary>
		/// Initializes the StateMachine's initial state.
		/// </summary>
		/// <param name="initialState">
		/// The state that will initially receive events from the StateMachine.
		/// </param>
		protected void InitializeStateMachine(State initialState)
		{
			#region Require

			if (initialState == null)
			{
				throw new ArgumentNullException("initialState");
			}
			if (m_initialized)
			{
				throw new InvalidOperationException("The state machine has already been initialized.");
			}

			#endregion

			m_initialized = true;
			s_currentStateMachine = this;

			State superstate = initialState;
			Stack<State> superstateStack = new Stack<State>();

			// If the initial state is a substate, travel up the state 
			// hierarchy in order to descend from the top state to the initial
			// state.
			while (superstate != null)
			{
				superstateStack.Push(superstate);
				superstate = superstate.Superstate;
			}

			// While there are superstates to traverse.
			while (superstateStack.Count > 0)
			{
				superstate = superstateStack.Pop();
				superstate.Entry();
			}

			m_currentState = initialState.EnterByHistory();
			s_currentStateMachine = this;

		}

		///<summary>
		/// Adds a new transition to the state machine.  The source and target states will be 
		/// implicitly added to the state machine if necesseray. 
		///</summary>
		///<param name="source">The source state.</param>
		///<param name="eventID">The event that will trigger the transition.</param>
		///<param name="target">The target state.</param>
		///<param name="actions">Optional actions that will be performed during the transition.</param>
		public void AddTransition(TState source, TEvent eventID, TState target, params ActionHandler[] actions)
		{
			States[source].Transitions.Add(eventID, States[target], actions);
		}
		///<summary>
		/// Adds a new transition to the state machine.  The source and target states will be 
		/// implicitly added to the state machine if necessary. 
		///</summary>
		///<param name="source">The source state.</param>
		///<param name="eventID">The event that will trigger the transition.</param>
		///<param name="guard">A transition guard.</param>
		///<param name="target">The target state.</param>
		///<param name="actions">Optional actions that will be performed during the transition.</param>
		public void AddTransition(TState source, TEvent eventID, GuardHandler guard, TState target, params ActionHandler[] actions)
		{
			States[source].Transitions.Add(eventID, guard, States[target], actions);
		}

		///<summary>
		/// Creates a new <see cref="State"/> object.
		///</summary>
		///<param name="stateID">The underlying state ID.</param>
		///<returns>The new <see cref="State"/> object.</returns>
		public State CreateState(TState stateID)
		{
			return new State(stateID);
		}

		///<summary>
		/// Creates a new <see cref="State"/> object.
		///</summary>
		///<param name="stateID">The underlying state ID.</param>
		///<param name="entryHandler">An entry handler that will be executed when entering the state.</param>
		///<param name="exitHandler">An exit handler that will be executed when leaving the state.</param>
		///<returns>The new <see cref="State"/> object.</returns>
		public State CreateState(TState stateID, EntryHandler entryHandler, ExitHandler exitHandler)
		{
			return new State(stateID, entryHandler, exitHandler);
		}

		///<summary>
		/// Sends an event to the state machine, that might trigger a transition.
		///</summary>
		///<param name="eventID">The event.</param>
		///<param name="args">Optional event arguments.</param>
		public abstract void Send(TEvent eventID, params object[] args);

		///<summary>
		/// Sends an event to the state machine, that might trigger a transition.
		/// This event will have precedence over other pending events that were sent using
		/// the <see cref="Send"/> method.
		///</summary>
		///<param name="eventID">The event.</param>
		///<param name="args">Optional event arguments.</param>
		protected abstract void SendPriority(TEvent eventID, params object[] args);


		///<summary>
		/// Setups substates for hierarchical state machine.
		///</summary>
		///<param name="superState">The super state.</param>
		///<param name="historyType">The history type.</param>
		///<param name="initialSubstate">The initial substate.</param>
		///<param name="additionalSubstates">Additional substates.</param>
		public void SetupSubstates(TState superState, HistoryType historyType, TState initialSubstate, params TState[] additionalSubstates)
		{
			States[superState].Substates.Add(States[initialSubstate]);
			foreach (TState substate in additionalSubstates)
			{
				States[superState].Substates.Add(States[substate]);
			}
			States[superState].HistoryType = historyType;
			States[superState].InitialState = States[initialSubstate];
		}

		/// <summary>
		/// Asserts that the state machine was initialized.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when the state machine was not initialized.</exception>
		protected virtual void AssertMachineIsValid()
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException("State machine was not initialized yet.");
			}
		}

		/// <summary>
		/// Raises the <see cref="StateMachine{TState,TEvent}.BeginDispatch"/> event.
		/// </summary>
		/// <param name="eventContext">The event context.</param>
		protected virtual void OnBeginDispatch(EventContext eventContext)
		{
			RaiseSafeEvent(BeginDispatch, new TransitionEventArgs<TState, TEvent>(eventContext), true);
		}

		/// <summary>
		/// Raises the <see cref="StateMachine{TState,TEvent}.BeginTransition"/> event.
		/// </summary>
		/// <param name="eventContext">The event context.</param>
		protected virtual void OnBeginTransition(EventContext eventContext)
		{
			RaiseSafeEvent(BeginTransition, new TransitionEventArgs<TState, TEvent>(eventContext), true);
		}

		/// <summary>
		/// Raises the <see cref="StateMachine{TState,TEvent}.TransitionCompleted"/> event.
		/// </summary>
		/// <param name="args">The <see cref="TransitionCompletedEventArgs{TState,TEvent}"/> instance 
		/// containing the event data.</param>
		protected virtual void OnTransitionCompleted(TransitionCompletedEventArgs<TState, TEvent> args)
		{
			RaiseSafeEvent(TransitionCompleted, args, true);
		}
		/// <summary>
		/// Raises the <see cref="StateMachine{TState,TEvent}.TransitionDeclined"/> event.
		/// </summary>
		/// <param name="eventContext">The event context.</param>
		protected virtual void OnTransitionDeclined(EventContext eventContext)
		{
			RaiseSafeEvent(TransitionDeclined, new TransitionEventArgs<TState, TEvent>(eventContext), true);
		}

		/// <summary>
		/// Raises the <see cref="StateMachine{TState,TEvent}.ExceptionThrown"/> event.
		/// </summary>
		/// <param name="args">The <see cref="TransitionCompletedEventArgs{TState,TEvent}"/> instance containing the event data.</param>
		protected virtual void OnExceptionThrown(TransitionErrorEventArgs<TState, TEvent> args)
		{
			RaiseSafeEvent(ExceptionThrown, args, false);
		}

		internal static void OnExceptionThrown(Exception ex)
		{
			s_currentStateMachine.OnExceptionThrown(
				new TransitionErrorEventArgs<TState, TEvent>(
					s_currentStateMachine.m_currentEventContext, ex));
		}

		internal static void OnBeginTransition()
		{
			s_currentStateMachine.OnBeginTransition(s_currentStateMachine.m_currentEventContext);
		}

		/// <summary>
		/// Raises an event, without throwing out exceptions. 
		/// Optionally, exceptions could trigger the <see cref="ExceptionThrown"/> event.
		/// </summary>
		/// <typeparam name="TArgs">The type of the event arguments.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="args">The event arguments.</param>
		/// <param name="raiseEventOnException">if set to <c>true</c> exceptions will trigger the 
		/// <see cref="ExceptionThrown"/> event.</param>
		protected void RaiseSafeEvent<TArgs>(EventHandler<TArgs> eventHandler, TArgs args, bool raiseEventOnException)
			where TArgs : EventArgs
		{
			try
			{
				if (eventHandler == null)
					return;
				eventHandler(this, args);
			}
			catch (Exception ex)
			{
				if (!raiseEventOnException) 
					return;
				OnExceptionThrown(
					new TransitionErrorEventArgs<TState, TEvent>(m_currentEventContext, ex));
			}
		}

		/// <summary>
		/// Dispatches events to the current state.
		/// </summary>
		/// <param name="eventID">
		/// The event ID.
		/// </param>
		/// <param name="args">
		/// The data accompanying the event.
		/// </param>
		protected virtual void Dispatch(TEvent eventID, object[] args)
		{
			// Reset action result.
			ActionResult = null;
			m_currentEventContext = new EventContext(CurrentStateID, eventID, args);
			s_currentStateMachine = this;
			try
			{
				OnBeginDispatch(m_currentEventContext);

				// Dispatch event to the current state.
				TransitionResult result = m_currentState.Dispatch(eventID, args);

/*
				// report errors
				if (result.Error != null)
					OnExceptionThrown(
						new TransitionErrorEventArgs(
							m_currentEventContext, result.Error));
*/

				// If a transition was fired as a result of this event.
				if (!result.HasFired)
				{
					OnTransitionDeclined(m_currentEventContext);
					return;
				}

				m_currentState = result.NewState;

				TransitionCompletedEventArgs<TState, TEvent> eventArgs =
					new TransitionCompletedEventArgs<TState, TEvent>(
						m_currentState.ID, m_currentEventContext, ActionResult, result.Error);

				OnTransitionCompleted(eventArgs);
			}
			catch (Exception ex)
			{
				HandleDispatchException(ex);
			}
			finally
			{
				m_currentEventContext = null;
				s_currentStateMachine = null;
			}
		}

		/// <summary>
		/// Template method for handling dispatch exceptions.
		/// </summary>
		/// <param name="ex">The exception.</param>
		protected abstract void HandleDispatchException(Exception ex);

		#endregion

		#region Properties

		/// <summary>
		/// Gets the ID of the current state.
		/// </summary>
		public TState CurrentStateID
		{
			get
			{
				#region Require

				if (!m_initialized)
				{
					throw new InvalidOperationException();
				}

				#endregion

				Debug.Assert(m_currentState != null);

				return m_currentState.ID;
			}
		}

		/// <summary>
		/// Gets or sets the results of the action performed during the last transition.
		/// </summary>
		/// <remarks>
		/// This property should only be set during the execution of an action method.
		/// </remarks>
		protected object ActionResult
		{
			get { return m_actionResult; }
			set { m_actionResult = value; }
		}

		///<summary>
		/// Gets the state machine type: active or passive.
		///</summary>
		public abstract StateMachineType StateMachineType { get; }

		/// <summary>
		/// Gets a value indicating whether this state machine is initialized.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this state machine is initialized; otherwise, <c>false</c>.
		/// </value>
		protected bool IsInitialized
		{
			get { return m_initialized && m_currentState != null; }
		}

		///<summary>
		/// The states of the state machine.
		///</summary>
		public StateMap States
		{
			get { return m_states; }
		}

		#endregion

		#endregion

		///<summary>
		/// A readonly mapping from <typeparamref name="TState"/> ID to 
		/// <see cref="Sanford.StateMachineToolkit.StateMachine{TState,TEvent}.State"/> object.
		///</summary>
		public class StateMap
		{
			private readonly Dictionary<TState, State> m_map = new Dictionary<TState, State>();

			/// <summary>
			/// Gets the <see cref="Sanford.StateMachineToolkit.StateMachine{TState,TEvent}.State"/> 
			/// object with the specified <typeparamref name="TState"/> ID.
			/// </summary>
			/// <value>The <typeparamref name="TState"/> ID.</value>
			public State this[TState state]
			{
				get
				{
					// lazy initialization
					if (!m_map.ContainsKey(state))
					{
						m_map.Add(state, new State(state));
					}
					return m_map[state];
				}
			}
		}

		///<summary>
		/// A context with information about an event is being processed by the state machine.
		///</summary>
		public class EventContext
		{
			private readonly TState m_sourceState;
			private readonly TEvent m_currentEvent;
			private readonly object[] m_args;

			/// <summary>
			/// Initializes a new instance of the <see cref="StateMachine{TState, TEvent}.EventContext"/> class.
			/// </summary>
			/// <param name="sourceState">The source state.</param>
			/// <param name="currentEvent">The current event.</param>
			/// <param name="args">The event arguments.</param>
			public EventContext(TState sourceState, TEvent currentEvent, object[] args)
			{
				m_sourceState = sourceState;
				m_currentEvent = currentEvent;
				m_args = args;
			}

			/// <summary>
			/// Gets the source state.
			/// </summary>
			/// <value>The state of the source.</value>
			public TState SourceState
			{
				[DebuggerStepThrough]
				get { return m_sourceState; }
			}

			/// <summary>
			/// Gets the event arguments.
			/// </summary>
			/// <value>The args.</value>
			public object[] Args
			{
				[DebuggerStepThrough]
				get { return m_args; }
			}

			/// <summary>
			/// Gets the current event.
			/// </summary>
			/// <value>The current event.</value>
			public TEvent CurrentEvent
			{
				[DebuggerStepThrough]
				get { return m_currentEvent; }
			}
		}
	}

	/// <summary>
	/// The type of the state machine.
	/// </summary>
	public enum StateMachineType
	{
		/// <summary>
		/// Passive state machine.
		/// </summary>
		Passive,
		/// <summary>
		/// Active state machine.
		/// </summary>
		Active
	}

}