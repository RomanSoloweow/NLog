﻿// 
// Copyright (c) 2004-2020 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
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

namespace NLog.UnitTests.Contexts
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class ScopeContextTest
    {
        [Fact]
        public void PushPropertyCaseInsensitiveTest()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue = "World";
            Dictionary<string, object> allProperties = null;
            var success = false;
            object value;

            // Act
            using (ScopeContext.PushProperty("HELLO", expectedValue))
            {
                success = ScopeContext.TryGetProperty("hello", out value);
                allProperties = ScopeContext.GetAllProperties().ToDictionary(x => x.Key, x => x.Value);
            }
            var failed = ScopeContext.TryGetProperty("hello", out var _);

            // Assert
            Assert.True(success);
            Assert.Equal(expectedValue, value);
            Assert.Single(allProperties);
            Assert.Equal(expectedValue, allProperties["HELLO"]);
            Assert.False(failed);
        }

        [Fact]
        public void PushPropertyNestedTest()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedString = "World";
            var expectedGuid = System.Guid.NewGuid();
            Dictionary<string, object> allProperties = null;
            object stringValueLookup1 = null;
            object stringValueLookup2 = null;
            bool stringValueLookup3 = false;
            object guidValueLookup1 = null;
            bool guidValueLookup2 = false;
            bool guidValueLookup3 = false;

            // Act
            using (ScopeContext.PushProperty("Hello", expectedString))
            {
                using (ScopeContext.PushProperty("RequestId", expectedGuid))
                {
                    ScopeContext.TryGetProperty("Hello", out stringValueLookup1);
                    ScopeContext.TryGetProperty("RequestId", out guidValueLookup1);
                    allProperties = ScopeContext.GetAllProperties().ToDictionary(x => x.Key, x => x.Value);
                }

                ScopeContext.TryGetProperty("Hello", out stringValueLookup2);
                guidValueLookup2 = ScopeContext.TryGetProperty("RequestId", out var _);
            }
            guidValueLookup3 = ScopeContext.TryGetProperty("RequestId", out var _);
            stringValueLookup3 = ScopeContext.TryGetProperty("Hello", out var _);

            // Assert
            Assert.Equal(2, allProperties.Count);
            Assert.Equal(expectedString, allProperties["Hello"]);
            Assert.Equal(expectedGuid, allProperties["RequestId"]);
            Assert.Equal(expectedString, stringValueLookup1);
            Assert.Equal(expectedString, stringValueLookup2);
            Assert.Equal(expectedGuid, guidValueLookup1);
            Assert.False(guidValueLookup2);
            Assert.False(guidValueLookup3);
            Assert.False(guidValueLookup3);
            Assert.False(stringValueLookup3);
        }

#if !NET3_5 && !NET4_0
        [Fact]
        public void PushNestedStatePropertiesTest()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedString = "World";
            var expectedGuid = System.Guid.NewGuid();
            var expectedProperties = new[] { new KeyValuePair<string, object>("Hello", expectedString), new KeyValuePair<string, object>("RequestId", expectedGuid) };
            var expectedNestedState = "First Push";
            Dictionary<string, object> allProperties = null;
            object[] allNestedStates = null;
            object stringValueLookup = null;

            // Act
            using (ScopeContext.PushProperty("Hello", "People"))
            {
                using (ScopeContext.PushNestedStateProperties(expectedNestedState, expectedProperties))
                {
                    allNestedStates = ScopeContext.GetAllNestedStates();
                    allProperties = ScopeContext.GetAllProperties().ToDictionary(x => x.Key, x => x.Value);
                }
                ScopeContext.TryGetProperty("Hello", out stringValueLookup);
            }

            // Assert
            Assert.Equal(2, allProperties.Count);
            Assert.Equal(expectedString, allProperties["Hello"]);
            Assert.Equal(expectedGuid, allProperties["RequestId"]);
            Assert.Single(allNestedStates);
            Assert.Equal(expectedNestedState, allNestedStates[0]);
            Assert.Equal("People", stringValueLookup);
        }
#endif

        [Fact]
        public void PushNestedStateTest()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedNestedState = "First Push";
            object topNestedState = null;
            object[] allNestedStates = null;

            // Act
            using (ScopeContext.PushNestedState(expectedNestedState))
            {
                topNestedState = ScopeContext.PeekNestedState();
                allNestedStates = ScopeContext.GetAllNestedStates();
            }
            var failed = ScopeContext.PeekNestedState() != null;

            // Assert
            Assert.Equal(expectedNestedState, topNestedState);
            Assert.Single(allNestedStates);
            Assert.Equal(expectedNestedState, allNestedStates[0]);
            Assert.False(failed);
        }

        [Fact]
        public void DoublePushNestedStateTest()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedNestedState1 = "First Push";
            var expectedNestedState2 = System.Guid.NewGuid();
            object topNestedState1 = null;
            object topNestedState2 = null;
            object[] allNestedStates = null;

            // Act
            using (ScopeContext.PushNestedState(expectedNestedState1))
            {
                topNestedState1 = ScopeContext.PeekNestedState();
                using (ScopeContext.PushNestedState(expectedNestedState2))
                {
                    topNestedState2 = ScopeContext.PeekNestedState();
                    allNestedStates = ScopeContext.GetAllNestedStates();
                }                   
            }
            var failed = ScopeContext.PeekNestedState() != null;

            // Assert
            Assert.Equal(expectedNestedState1, topNestedState1);
            Assert.Equal(expectedNestedState2, topNestedState2);
            Assert.Equal(2, allNestedStates.Length);
            Assert.Equal(expectedNestedState2, allNestedStates[0]);
            Assert.Equal(expectedNestedState1, allNestedStates[1]);
            Assert.False(failed);
        }

        [Fact]
        public void ClearScopeContextTest()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedNestedState = "First Push";
            var expectedString = "World";
            var expectedGuid = System.Guid.NewGuid();
            object[] allNestedStates1 = null;
            object[] allNestedStates2 = null;
            object stringValueLookup1 = null;
            object stringValueLookup2 = null;

            // Act
            using (ScopeContext.PushProperty("Hello", expectedString))
            {
                using (ScopeContext.PushProperty("RequestId", expectedGuid))
                {
                    using (ScopeContext.PushNestedState(expectedNestedState))
                    {
                        ScopeContext.Clear();
                        allNestedStates1 = ScopeContext.GetAllNestedStates();
                        ScopeContext.TryGetProperty("Hello", out stringValueLookup1);
                    }
                }

                // Original scope was restored on dispose, verify expected behavior
                allNestedStates2 = ScopeContext.GetAllNestedStates();
                ScopeContext.TryGetProperty("Hello", out stringValueLookup2);
            }

            // Assert
            Assert.Null(stringValueLookup1);
            Assert.Equal(expectedString, stringValueLookup2);
            Assert.Empty(allNestedStates1);
            Assert.Empty(allNestedStates2);
        }

        [Fact]
        public void LegacyNdlcPopShouldNotAffectProperties1()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue = "World";
            var success = false;
            object propertyValue;

            // Act
            using (ScopeContext.PushProperty("Hello", expectedValue))
            {
                NestedDiagnosticsLogicalContext.PopObject();    // Should not pop anything (skip legacy mode)
                success = ScopeContext.TryGetProperty("Hello", out propertyValue);
            }

            // Assert
            Assert.True(success);
            Assert.Equal(expectedValue, propertyValue);
        }

        [Fact]
        public void LegacyNdlcPopShouldNotAffectProperties2()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue = "World";
            var expectedNestedState = "First Push";
            var success = false;
            object propertyValue;
            object nestedState;

            // Act
            using (ScopeContext.PushProperty("Hello", expectedValue))
            {
                ScopeContext.PushNestedState(expectedNestedState);
                nestedState = NestedDiagnosticsLogicalContext.PopObject();    // Should only pop active scope (skip legacy mode)
                success = ScopeContext.TryGetProperty("Hello", out propertyValue);
            }

            // Assert
            Assert.True(success);
            Assert.Equal(expectedValue, propertyValue);
            Assert.Equal(expectedNestedState, nestedState);
        }

        [Fact]
        public void LegacyNdlcPopShouldNotAffectProperties3()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue1 = "World";
            var expectedValue2 = System.Guid.NewGuid();
            var expectedNestedState1 = "First Push";
            var expectedNestedState2 = System.Guid.NewGuid();
            var success1 = false;
            var success2 = false;
            object propertyValue1;
            object propertyValue2;
            object nestedState1;
            object nestedState2;

            // Act
            using (ScopeContext.PushProperty("Hello", expectedValue1))
            {
                ScopeContext.PushNestedState(expectedNestedState1);
                ScopeContext.PushNestedState(expectedNestedState2);
                using (ScopeContext.PushProperty("RequestId", expectedValue2))
                {
                    nestedState2 = NestedDiagnosticsLogicalContext.PopObject();    // Evil pop where it should leave properties alone (Legacy mode)
                    nestedState1 = NestedDiagnosticsLogicalContext.PopObject();    // Evil pop where it should leave properties alone (Legacy mode)

                    success1 = ScopeContext.TryGetProperty("Hello", out propertyValue1);
                    success2 = ScopeContext.TryGetProperty("RequestId", out propertyValue2);
                }
            }

            // Assert
            Assert.True(success1);
            Assert.True(success2);
            Assert.Equal(expectedValue1, propertyValue1);
            Assert.Equal(expectedValue2, propertyValue2);
            Assert.Equal(expectedNestedState1, nestedState1);
            Assert.Equal(expectedNestedState2, nestedState2);
        }

        [Fact]
        public void LegacyNdlcClearShouldNotAffectProperties1()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue = "World";
            var success = false;
            object propertyValue;

            // Act
            using (ScopeContext.PushProperty("Hello", expectedValue))
            {
                NestedDiagnosticsLogicalContext.Clear();    // Should not clear anything (skip legacy mode)
                success = ScopeContext.TryGetProperty("Hello", out propertyValue);
            }

            // Assert
            Assert.True(success);
            Assert.Equal(expectedValue, propertyValue);
        }

        [Fact]
        public void LegacyNdlcClearShouldNotAffectProperties2()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue = "World";
            var expectedNestedState = "First Push";
            var success = false;
            object propertyValue;

            // Act
            using (ScopeContext.PushProperty("Hello", expectedValue))
            {
                ScopeContext.PushNestedState(expectedNestedState);
                NestedDiagnosticsLogicalContext.Clear();    // Should not clear properties (Legacy mode)
                success = ScopeContext.TryGetProperty("Hello", out propertyValue);
            }

            // Assert
            Assert.True(success);
            Assert.Equal(expectedValue, propertyValue);
        }

        [Fact]
        public void LegacyMdlcClearShouldNotAffectStackValues1()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedNestedState = "First Push";
            object[] allNestedStates = null;

            // Act
            using (ScopeContext.PushNestedState(expectedNestedState))
            {
                MappedDiagnosticsLogicalContext.Clear();    // Should not clear anything (skip legacy mode)
                allNestedStates = ScopeContext.GetAllNestedStates();
            }

            // Assert
            Assert.Single(allNestedStates);
            Assert.Equal(expectedNestedState, allNestedStates[0]);
        }

        [Fact]
        public void LegacyMdlcClearShouldNotAffectStackValues2()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue = "World";
            var expectedNestedState = "First Push";
            object[] allNestedStates = null;

            // Act
            using (ScopeContext.PushNestedState(expectedNestedState))
            {
                ScopeContext.PushProperty("Hello", expectedValue);
                MappedDiagnosticsLogicalContext.Clear();    // Should not clear stack (Legacy mode)
                allNestedStates = ScopeContext.GetAllNestedStates();
            }

            // Assert
            Assert.Single(allNestedStates);
            Assert.Equal(expectedNestedState, allNestedStates[0]);
        }

        [Fact]
        public void LegacyMdlcRemoveShouldNotAffectStackValues1()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedNestedState = "First Push";
            object[] allNestedStates = null;

            // Act
            using (ScopeContext.PushNestedState(expectedNestedState))
            {
                MappedDiagnosticsLogicalContext.Remove("Hello");    // Should not remove anything (skip legacy mode)
                allNestedStates = ScopeContext.GetAllNestedStates();
            }

            // Assert
            Assert.Single(allNestedStates);
            Assert.Equal(expectedNestedState, allNestedStates[0]);
        }

        [Fact]
        public void LegacyMdlcRemoveShouldNotAffectStackValues2()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue1 = "World";
            var expectedValue2 = System.Guid.NewGuid();
            var expectedNestedState1 = "First Push";
            var expectedNestedState2 = System.Guid.NewGuid();
            object propertyValue1;
            object propertyValue2;
            object[] allNestedStates = null;
            var success1 = false;
            var success2 = false;

            // Act
            using (ScopeContext.PushNestedState(expectedNestedState1))
            {
                using (ScopeContext.PushProperty("Hello", expectedValue1))
                {
                    using (ScopeContext.PushNestedState(expectedNestedState2))
                    {
                        ScopeContext.PushProperty("RequestId", expectedValue2);
                        MappedDiagnosticsLogicalContext.Remove("RequestId");    // Should not change stack (Legacy mode)
                        allNestedStates = ScopeContext.GetAllNestedStates();

                        success1 = ScopeContext.TryGetProperty("Hello", out propertyValue1);
                        success2 = ScopeContext.TryGetProperty("RequestId", out propertyValue2);
                    }
                }
            }

            // Assert
            Assert.Equal(2, allNestedStates.Length);
            Assert.Equal(expectedNestedState2, allNestedStates[0]);
            Assert.Equal(expectedNestedState1, allNestedStates[1]);
            Assert.True(success1);
            Assert.False(success2);
            Assert.Equal(expectedValue1, propertyValue1);
            Assert.Null(propertyValue2);
        }

        [Fact]
        public void LegacyMdlcSetShouldNotAffectStackValues1()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue = "World";
            var expectedNestedState = "First Push";
            object propertyValue;
            object[] allNestedStates = null;
            var success = false;

            // Act
            using (ScopeContext.PushNestedState(expectedNestedState))
            {
                MappedDiagnosticsLogicalContext.Set("Hello", expectedValue);    // Skip legacy mode (normal property push)
                success = ScopeContext.TryGetProperty("Hello", out propertyValue);
                allNestedStates = ScopeContext.GetAllNestedStates();
            }

            // Assert
            Assert.Single(allNestedStates);
            Assert.Equal(expectedNestedState, allNestedStates[0]);
            Assert.True(success);
            Assert.Equal(expectedValue, propertyValue);
        }

        [Fact]
        public void LegacyMdlcSetShouldNotAffectStackValues2()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue = "World";
            var expectedNestedState = "First Push";
            object propertyValue;
            object[] allNestedStates = null;
            var success = false;

            // Act
            using (ScopeContext.PushNestedState(expectedNestedState))
            {
                using (ScopeContext.PushProperty("Hello", expectedValue))
                {
                    MappedDiagnosticsLogicalContext.Set("Hello", expectedValue);    // Skip legacy mode (ignore when same value)
                    success = ScopeContext.TryGetProperty("Hello", out propertyValue);
                    allNestedStates = ScopeContext.GetAllNestedStates();
                }
            }

            // Assert
            Assert.Single(allNestedStates);
            Assert.Equal(expectedNestedState, allNestedStates[0]);
            Assert.True(success);
            Assert.Equal(expectedValue, propertyValue);
        }

        [Fact]
        public void LegacyMdlcSetShouldNotAffectStackValues3()
        {
            // Arrange
            ScopeContext.Clear();
            var expectedValue = "Bob";
            var expectedNestedState = "First Push";
            object propertyValue1;
            object propertyValue2;
            object[] allNestedStates = null;
            var success1 = false;
            var success2 = false;

            // Act
            using (ScopeContext.PushNestedState(expectedNestedState))
            {
                using (ScopeContext.PushProperty("Hello", "World"))
                {
                    MappedDiagnosticsLogicalContext.Set("Hello", expectedValue);    // Enter legacy mode (need to overwrite)
                    success1 = ScopeContext.TryGetProperty("Hello", out propertyValue1);
                    allNestedStates = ScopeContext.GetAllNestedStates();
                }

                success2 = ScopeContext.TryGetProperty("Hello", out propertyValue2);
            }

            // Assert
            Assert.Single(allNestedStates);
            Assert.Equal(expectedNestedState, allNestedStates[0]);
            Assert.True(success1);
            Assert.Equal(expectedValue, propertyValue1);
            Assert.False(success2);
            Assert.Null(propertyValue2);
        }
    }
}
