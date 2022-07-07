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
using FLS.MembershipFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLS
{
	public class MoMDefuzzification : IDefuzzification
	{
		public Double Defuzzify(List<IMembershipFunction> functions)
		{
			var minX = functions.Select(f => f.Min()).Min();
			var maxX = functions.Select(f => f.Max()).Max();

			var max = 0.0;
			var startMax = 0.0;
			var len = 0.0;

			for (var i = minX; i <= maxX; i += 1)
			{
				var maxFuzVal = functions.Select(f=>f.PremiseModifier * f.Fuzzify(i)).Max();
				if (max < maxFuzVal)
				{
					max = maxFuzVal;
					startMax = i;
					len = 0.0;
				}
				else if (max == maxFuzVal && 0 < maxFuzVal)
				{
					len++;
				}
			}

			var mid = startMax + (len / 2.0);

			return mid;
		}
	}
}
