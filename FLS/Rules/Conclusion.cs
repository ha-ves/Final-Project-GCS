﻿#region License
//   FLS - Fuzzy Logic Sharp for .NET
//   Copyright 2014 David Grupp
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
#endregion
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using FLS.Interfaces;
using FLS.Variables;

namespace FLS.Rules
{
    /// <summary>
    /// The then-part of the rule
    /// </summary>
    public class Conclusion : FuzzyRuleClause
	{
		public Conclusion(LinguisticVariable variable, IFuzzyRuleToken @operator, IMembershipFunction function)
			: base(variable, @operator, function)
		{
		}

	}
}
