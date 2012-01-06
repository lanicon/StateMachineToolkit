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

namespace Sanford.Threading
{
    public abstract class CompletedEventArgs : EventArgs
    {
        private readonly Exception error;

        protected CompletedEventArgs(Exception error)
        {
            this.error = error;
        }

        public Exception Error
        {
            get { return error; }
        }
    }

    /// <summary>
	/// Represents information about the InvokeCompleted event.
	/// </summary>
	public sealed class InvokeCompletedEventArgs : CompletedEventArgs
	{
		private readonly Delegate method;

		private readonly object[] args;

		private readonly object result;

        public InvokeCompletedEventArgs(Delegate method, object[] args, object result, Exception error)
            : base(error)
		{
			this.method = method;
			this.args = args;
			this.result = result;
		}

		public object[] GetArgs()
		{
			return args;
		}

		public Delegate Method
		{
			get { return method; }
		}

		public object Result
		{
			get { return result; }
		}
	}
}