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
using FLS.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLS.MembershipFunctions
{
	public class CompositeMembershipFunction : BaseMembershipFunction
	{
		public CompositeMembershipFunction(String name, IMembershipFunction leftFunction, IMembershipFunction rightFunction, double midPoint)
			: base(name)
		{
			_leftFunction = leftFunction;
			_rightFunction = rightFunction;
			_midPoint = midPoint;
		}

		private IMembershipFunction _leftFunction;
		private IMembershipFunction _rightFunction;
		private double _midPoint;

		#region Public Methods

		public override Double Fuzzify(Double inputValue)
		{
			if (inputValue <= _midPoint)
			{
				return _leftFunction.Fuzzify(inputValue);
			}
			else
			{
				return _rightFunction.Fuzzify(inputValue);
			}
		}

		public override Double Min()
		{
			return _leftFunction.Min();
		}

		public override Double Max()
		{
			return _rightFunction.Max();
		}

		#endregion

	}
}
