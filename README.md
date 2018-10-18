EdlibNet
========

This is a .NET Core implementation of [edlib](https://github.com/Martinsos/edlib), a lightweight and super fast C/C++ library for sequence alignment using [edit distance](https://en.wikipedia.org/wiki/Edit_distance).

Calculating edit distance of two strings is as simple as:

```csharp
Align("hello", "world!", GetDefaultConfig());
```

## Contents
- [Features](#features)
- [Alignment methods](#alignment-methods)
- [How to use](#how-to-use)
- [TODO](#todo)
- [Acknowledgements](#acknowledgements)

## Features

Most of texts from this README are copied from the original library.

* Use little memory.
* Calculates **edit distance (Levehnstein distance)**.
* ~~It can find **optimal alignment path** (instructions how to transform first sequence into the second sequence).~~
* ~~It can find just the **start and/or end locations of alignment path** - can be useful when speed is more important than having exact alignment path.~~
* ~~Supports **multiple [alignment methods](#alignment-methods)**: global(**NW**), prefix(**SHW**) and infix(**HW**), each of them useful for different scenarios.~~
* You can **extend character equality definition**, enabling you to e.g. have wildcard characters, to have case insensitive alignment or to work with degenerate nucleotides.
* It can easily handle small or **very large sequences**, even when finding alignment path, while consuming very little memory.
* **Super fast** thanks to Myers's bit-vector algorithm.

## Alignment methods

Copied from the original library:

> Edlib supports 3 alignment methods:
> * **global (NW)** - This is the standard method, when we say "edit distance" this is the method that is assumed.
>  It tells us the smallest number of operations needed to transform first sequence into second sequence.
>  *This method is appropriate when you want to find out how similar is first sequence to second sequence.*
>* **prefix (SHW)** - Similar to global method, but with a small twist - gap at query end is not penalized. What that means is that deleting elements from the end of second sequence is "free"!
>  For example, if we had `AACT` and `AACTGGC`, edit distance would be 0, because removing `GGC` from the end of second sequence is "free" and does not count into total edit distance.
>  *This method is appropriate when you want to find out how well first sequence fits at the beginning of second sequence.*
>* **infix (HW)**: Similar as prefix method, but with one more twist - gaps at query end **and start** are not penalized. What that means is that deleting elements from the start and end of second sequence is "free"!
>  For example, if we had `ACT` and `CGACTGAC`, edit distance would be 0, because removing `CG` from the start and `GAC` from the end of second sequence is "free" and does not count into total edit distance.
>  *This method is appropriate when you want to find out how well first sequence fits at any part of second sequence.* For example, if your second sequence was a long text and your first sequence was a sentence from that text, but slightly scrambled, you could use this method to discover how scrambled it is and where it fits in that text.
>  *In bioinformatics, this method is appropriate for aligning read to a sequence.*

## How to use
Just load this project into yours

## TODO
- [ ] Finish implement prefix(**SHW**) and infix(**HW**) alignment modes.
- [ ] Implement finding the optimal alignment path.
- [ ] Implement finding start and/or end locations of the alignment path.
- [ ] Unit tests.
- [ ] Benchmark.
- [ ] Publish to NuGet.

## Acknowledgements

Martin Šošić (@Martinsos) - Creator of the original project.

Mile Šikić (@msikic) - Mentoring and guidance through whole project.

Ivan Sović (@isovic) - Help with testing and prioritizing features, valuable comments on the manuscript.