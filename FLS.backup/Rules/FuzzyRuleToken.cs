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
using System.Linq;
using System.Text;

namespace FLS.Rules
{
	/// <summary>
	/// Individual Token for a fuzzy rule
	/// </summary>
	public class FuzzyRuleToken : IFuzzyRuleToken
	{
		public FuzzyRuleToken()
		{

		}

		public FuzzyRuleToken(String value, FuzzyRuleTokenType type)
		{
			Name = value;
			Type = type;
		}


		public String Name { get; set; }
		public FuzzyRuleTokenType Type { get; set; }
	}



}
