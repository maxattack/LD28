// Little Polygon SDK
// Copyright (C) 2013 Max Kaufmann
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LP = CustomBehaviour;

// Logically similar to C#'s BitArray class, except our iterator is design
// to efficiently list the indices of "1s" rather than every single bit, 
// even when the bitvector is sparse, since this is intended to enumerate
// "active" slots in block-allocated structures.

// Furthermore, it's And/Or/Xor interface does not have any side-effects and modifies
// sets in-place without additional dynamic memory allocation.

// In theory we could scalar-optimize this by inlining the words instead of 
// allocating an array and using unsafe pointer arithmetic.  I consider this a
// "nuclear option" rather than a rational policy.

public class Bitset {
	public static int clz(int x) {
		// Count Leading Zeroes (similar to C's __builtin_clz intrinsic)
		x |= (x >> 1);
		x |= (x >> 2);
		x |= (x >> 4);
		x |= (x >> 8);
		x |= (x >> 16);
		return 32 - ones(x);
	}
	
	public static int ones(int x) {
		// Count Ones (similar to C's __builtin_popcount intrinsic)
		x -= ((x >> 1) & 0x55555555);
		x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
		x = (((x >> 4) + x) & 0x0f0f0f0f);
		x += (x >> 8);
		x += (x >> 16);
		return x & 0x0000003f;
	}
	
	public static int lz(int bit) { 
		// Make a mask of "bit" many leading zeroes
		return 1 << (31-bit);
	}

	// this mask identifies which words are nonzero, which we can enumerate with bit tricks
	int nonzeroWords;

	// each word is a run of 32-bits, which we also enumerate with bit-tricks.
	int[] words;
	
	public Bitset(int aCapacity) {
		// don't want to deal with "remainders"
		LP.Assert(aCapacity > 0, "Capacity is Nonzero");
		LP.Assert(aCapacity % 32 == 0, "Capacity is a Multiple of 32");
		LP.Assert(aCapacity <= 32 * 32, "Capacity is at most 1024");
		words = new int[ aCapacity/32 ];
		nonzeroWords = 0;
	}
	
	public int Size { get { return 32 * words.Length; } }

	public void Mark(int index) {
		int word = index >> 5;
		int bit = index & 31;
		nonzeroWords |= lz (word);
		words[word] |= lz(bit);
	}
	
	public void Clear(int index) {
		int word = index >> 5;
		int bit = index & 31;
		words[word] &= ~lz(bit);
		if (words[word] == 0) { 
			nonzeroWords &= ~lz (word); 
		}
	}
	
	public void Mark() {
		int NUM_WORDS = words.Length;
		unchecked {
			for(int w=0; w<NUM_WORDS; ++w) {
				nonzeroWords |= lz (w);
				words[w] = (int)0xFFFFFFFF;
			}
		}
	}
	
	public void Clear() {
		int NUM_WORDS = words.Length;
		nonzeroWords = 0;
		for(int w=0; w<NUM_WORDS; ++w) {
			words[w] = 0;
		}
	}
	
	public bool this[int i] {
		get {
			int word = i >> 5;
			int bit = i & 31;
			return (words[word] & lz(bit)) != 0;
		}
	}
	
	public bool Empty {
		get { return nonzeroWords == 0; }
	}
	
	public int Count {
		get {
			int c = 0;
			int remainder = nonzeroWords;
			while(remainder != 0) {
				int w = clz (remainder);
				remainder ^= lz (w);
				c += ones (words[w]);
			}
			return c;
		}
	}

	public Bitset Clone() {
		var result = new Bitset(Size);
		result.nonzeroWords = nonzeroWords;
		int remainder = nonzeroWords;
		while(remainder != 0) {
			int w = clz(remainder);
			remainder ^= lz(w);
			result.words[w] = words[w];
		}
		return result;
	}

	public bool FindFirst(out int index) {
		if (nonzeroWords != 0) {
			int w = clz (nonzeroWords);
			int v = words[w];
			index = (w << 5) | clz (v);
			return true;
		} else {
			index = 0;
			return false;
		}
	}
	
	public bool ClearFirst(out int index) {
		if (nonzeroWords != 0) {
			int w = clz (nonzeroWords);
			int v = words[w];
			int bit = clz (v);
			index = (w << 5) | bit;
			words[w] ^= lz (bit);
			if (words[w] == 0) {
				nonzeroWords &= ~lz (w);
			}
			return true;
		} else {
			index = 0;
			return false;
		}
	}
	
	public IEnumerable<int> ListBits() {
		int remainder = nonzeroWords;
		while(remainder != 0) {
			int w = clz (remainder);
			remainder ^= lz (w);
			int v = words[w];
			while(v != 0) {
				int bit = clz (v);
				v ^= lz (bit);
				yield return (w << 5) | bit;
			}
		}
	}

	// Semantically equivalent to the previous method, 
	// but optimized for use in a critical sections.
	public struct BitLister {
		readonly Bitset bs;
		int remainder, w, v;

		public BitLister(Bitset bitset) {
			bs = bitset;
			remainder = bs.nonzeroWords;
			if (remainder != 0) {
				w = clz (remainder);
				v = bs.words[w];
			} else {
				w = 0;
				v = 0;
			}
		}

		public bool Next(out int idx) {
			if (remainder == 0) {
				idx = -1;
				return false;
			}
			int bit = clz (v);
			v ^= lz (bit);
			if (v == 0) {
				remainder ^= lz (w);
				if (remainder != 0) {
					w = clz (remainder);
					v = bs.words[w];
				}
			}
			idx = (w << 5) | bit;
			return true;
		}

		public void Reset() {
			remainder = bs.nonzeroWords;
			if (remainder != 0) {
				w = clz (remainder);
				v = bs.words[w];
			} else {
				w = 0;
				v = 0;
			}
		}
	}
	
	public void Union(Bitset other) {
		LP.Assert(words.Length == other.words.Length, "Bitset Sizes must match");
		int remainder = other.nonzeroWords;
		nonzeroWords |= other.nonzeroWords;
		while(remainder != 0) {
			int w = clz (remainder);
			remainder ^= lz (w);
			words[w] |= other.words[w];
		}
	}
	
	public void Intersect(Bitset other) {
		LP.Assert(words.Length == other.words.Length, "Bitset Sizes must match");
		int remainder = other.nonzeroWords;
		while(remainder != 0) {
			int w = clz (remainder);
			remainder ^= lz (w);
			words[w] &= other.words[w];
			if (words[w] == 0) {
				nonzeroWords &= ~lz (w);
			}
		}
	}
	
	public void Xor(Bitset other) {
		LP.Assert(words.Length == other.words.Length, "Bitset Sizes must match");
		int remainder = other.nonzeroWords;
		while(remainder != 0) {
			int w = clz (remainder);
			remainder ^= lz (w);
			words[w] ^= other.words[w];
			if (words[w] == 0) {
				nonzeroWords &= ~lz (w);
			} else {
				nonzeroWords |= lz (w);
			}
		}
	}
	
	public void Negate() {
		int NUM_WORDS = words.Length;
		nonzeroWords = 0;
		for(int w=0; w<NUM_WORDS; ++w) {
			words[w] = ~words[w];
			if (words[w] != 0) {
				nonzeroWords |= lz (w);
			}
		}

	}
}

