/* Copyright (c) 2009 Joseph Robert. All rights reserved.
 *
 * This file is part of BloomFilter.NET.
 * 
 * BloomFilter.NET is free software; you can redistribute it and/or 
 * modify it under the terms of the GNU Lesser General Public 
 * License as published by the Free Software Foundation; either 
 * version 3.0 of the License, or (at your option) any later 
 * version.
 * 
 * BloomFilter.NET is distributed in the hope that it will be 
 * useful, but WITHOUT ANY WARRANTY; without even the implied 
 * warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
 * See the GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with BloomFilter.NET.  If not, see 
 * <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using DataTypes;

namespace DataTypes.BloomFilterExample
{
    class BloomFilterExample
    {
        static void Main()
        {
            BloomFilter<string> bf = new BloomFilter<string>(20, 3);

            bf.Add("testing");
            bf.Add("nottesting");
            bf.Add("testingagain");

            Console.WriteLine(bf.Contains("badstring")); // False
            Console.WriteLine(bf.Contains("testing")); // True

            List<string> testItems = new List<string>() { "badstring", "testing", "test" };

            Console.WriteLine(bf.ContainsAll(testItems)); // False
            Console.WriteLine(bf.ContainsAny(testItems)); // True

            // False Positive Probability: 0.040894188143892
            Console.WriteLine("False Positive Probability: " + bf.FalsePositiveProbability());
        }
    }
}
