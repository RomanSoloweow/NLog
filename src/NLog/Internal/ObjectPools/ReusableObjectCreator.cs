// 
// Copyright (c) 2004-2021 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;

namespace NLog.Internal
{
    /// <summary>
    /// Controls a single allocated object for reuse (only one active user)
    /// </summary>
    internal class ReusableObjectCreator<T> where T : class
    {
        protected T _reusableObject;
        private readonly Action<int, int, T> _clearObject;
        private readonly Func<int, int, T> _createObject;
        private readonly int _initialCapacity;
        private readonly int _maxCapacity;

        protected ReusableObjectCreator(int initialCapacity, int maxCapacity, Func<int, int, T> createObject, Action<int, int, T> clearObject)
        {
            _reusableObject = createObject(initialCapacity, maxCapacity);
            _clearObject = clearObject;
            _createObject = createObject;
            _initialCapacity = initialCapacity;
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// Creates handle to the reusable char[]-buffer for active usage
        /// </summary>
        /// <returns>Handle to the reusable item, that can release it again</returns>
        public LockOject Allocate()
        {
            var reusableObject = _reusableObject ?? _createObject(_initialCapacity, _maxCapacity);
            System.Diagnostics.Debug.Assert(_reusableObject != null);
            _reusableObject = null;
            return new LockOject(this, reusableObject);
        }

        private void Deallocate(T reusableObject)
        {
            _clearObject(_initialCapacity, _maxCapacity, reusableObject);
            _reusableObject = reusableObject;
        }

        public struct LockOject : IDisposable
        {
            /// <summary>
            /// Access the acquired reusable object 
            /// </summary>
            public readonly T Result;
            private readonly ReusableObjectCreator<T> _owner;

            public LockOject(ReusableObjectCreator<T> owner, T reusableObject)
            {
                Result = reusableObject;
                _owner = owner;
            }

            public void Dispose()
            {
                _owner?.Deallocate(Result);
            }
        }
    }
}
