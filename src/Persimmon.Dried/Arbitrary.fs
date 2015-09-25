﻿namespace Persimmon.Dried

type NonShrinkerArbitrary<'T> = {
  Gen: Gen<'T>
  PrettyPrinter: 'T -> Pretty
}

type Arbitrary<'T> = {
  Gen: Gen<'T>
  Shrinker: Shrink<'T>
  PrettyPrinter: 'T -> Pretty
}
with
  member this.NonShrinker: NonShrinkerArbitrary<'T> = {
    Gen = this.Gen
    PrettyPrinter = this.PrettyPrinter
  }

[<AutoOpen>]
module ArbitrarySyntax =

  type GenBuilder with
    member inline __.Source(arb: Arbitrary<_>) = arb.Gen
    member inline __.Source(arb: NonShrinkerArbitrary<_>) = arb.Gen
    member inline __.Source(gen: Gen<_>) = gen

[<RequireQualifiedAccess>]
module Arb =

  let unit = {
    Gen = Gen.constant ()
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = fun () -> Pretty(fun _ -> "unit")
  }

  [<CompiledName("Bool")>]
  let bool = {
    Gen = Gen.elements [ true; false ]
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }

  open FsRandom
  open System

  [<CompiledName("Byte")>]
  let byte = {
    Gen = Gen.choose (ruint8)
    Shrinker = Shrink.shrinkByte
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("UInt16")>]
  let uint16 = {
    Gen = Gen.choose (ruint16)
    Shrinker = Shrink.shrinkUInt16
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("UInt32")>]
  let uint32 = {
    Gen = Gen.choose (ruint32)
    Shrinker = Shrink.shrinkUInt32
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("UInt64")>]
  let uint64 = {
    Gen = Gen.choose (ruint64)
    Shrinker = Shrink.shrinkUInt64
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("SByte")>]
  let sbyte = {
    Gen = Gen.choose rint8
    Shrinker = Shrink.shrinkSbyte
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("Int16")>]
  let int16 = {
    Gen = Gen.choose rint16
    Shrinker = Shrink.shrinkInt16
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("Int")>]
  let int = {
    Gen = Gen.choose rint32
    Shrinker = Shrink.shrinkInt
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("Int64")>]
  let int64 = {
    Gen = Gen.choose rint64
    Shrinker = Shrink.shrinkInt64
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("Single")>]
  let float32 = {
    Gen = gen {
      let! s = Gen.choose (Statistics.uniformDiscrete (0, 1))
      let! e = Gen.choose (Statistics.uniformDiscrete (0, 0xfe))
      let! m = Gen.choose (Statistics.uniformDiscrete (0, 0x7fffff))
      return System.Convert.ToSingle((s <<< 31) ||| (e <<< 23) ||| m)
    }
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }

  let list a = {
    Gen = Gen.listOf a.Gen
    Shrinker = Shrink.shrinkList a.Shrinker
    PrettyPrinter = Pretty.prettyList
  }

  let nonEmptyList a = {
    Gen = Gen.nonEmptyListOf a.Gen
    Shrinker = Shrink.shrinkList a.Shrinker
    PrettyPrinter = Pretty.prettyList
  }

  [<CompiledName("IEnumerable")>]
  let seq s = {
    Gen = Gen.seqOf s.Gen
    Shrinker = Shrink.shrinkSeq s.Shrinker
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("Array")>]
  let array a = {
    Gen = Gen.arrayOf a.Gen
    Shrinker = Shrink.shrinkArray a.Shrinker
    PrettyPrinter = Pretty.prettyAny
  }

  let set s = {
    Gen = Gen.listOf s.Gen |> Gen.map Seq.ofList
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }

  let map key value = {
    Gen =
      Gen.size
      |> Gen.bind (fun n ->
        Gen.listOfLength n key.Gen
        |> Gen.bind (fun k ->
          Gen.listOfLength n value.Gen
          |> Gen.map (fun v -> List.zip k v |> Map.ofList)))
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }

  open System.Linq
  open System.Collections.Generic

  [<CompiledName("List")>]
  let resizeArray xs = {
    Gen = Gen.listOf xs.Gen |> Gen.map Enumerable.ToList
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("ICollection")>]
  let icollection cs = {
    Gen = (resizeArray cs).Gen |> Gen.map (fun xs -> xs :> ICollection<_>)
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("Dictionary")>]
  let dict (key, value) = {
    Gen = (map key value).Gen |> Gen.map (fun m -> Dictionary<_,_>(m))
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("Char")>]
  let char = {
    Gen = Gen.frequency
      [
        (0xD800 - Operators.int Char.MinValue,
          Gen.choose (Statistics.uniformDiscrete (Operators.int Char.MinValue, 0xD800 - 1)) |> Gen.map char)
        (Operators.int Char.MaxValue - 0xDFFF,
          Gen.choose (Statistics.uniformDiscrete (0xDFFF + 1, Operators.int Char.MaxValue)) |> Gen.map char)
      ]
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("String")>]
  let string = {
    Gen = (array char).Gen |> Gen.map (fun xs -> String(xs))
    Shrinker = Shrink.shrinkString
    PrettyPrinter = Pretty.prettyString
  }

  [<CompiledName("DateTime")>]
  let datetime fmt = {
    Gen = gen {
      let! l = int64
      let d = DateTime.MinValue
      return DateTime(d.Ticks + l)
    }
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyDateTime fmt
  }

  let func (c: CoArbitrary<_>) (a: Arbitrary<_>) = {
    Gen = Gen.promote (fun x -> CoArb.apply x c a.Gen)
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }

  [<CompiledName("Func")>]
  let systemFunc (c, a) =
    let arb = func c a
    {
      Gen = arb.Gen |> Gen.map (fun f -> Func<_, _>(f))
      Shrinker = Shrink.shrinkAny
      PrettyPrinter = Pretty.prettyAny
    }

  [<CompiledName("Guid")>]
  let guid = {
    Gen = gen {
      let! a = int
      let! b = int16
      let! c = int16
      let! d = byte
      let! e = byte
      let! f = byte
      let! g = byte
      let! h = byte
      let! i = byte
      let! j = byte
      let! k = byte
      return Guid(a, b, c, d, e, f, g, h, i, j, k)
    }
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyGuid
  }

  let option (a: Arbitrary<_>) = {
    Gen = Gen.sized(fun n ->
      Gen.frequency [
        (n, Gen.resize (n / 2) a.Gen |> Gen.map Some)
        (1, Gen.constant None)
      ])
    Shrinker = Shrink.shrinkOption a.Shrinker
    PrettyPrinter = Pretty.prettyAny
  }

  let choice (at: Arbitrary<_>) (au: Arbitrary<_>) = {
    Gen = Gen.oneOf [ Gen.map Choice1Of2 at.Gen; Gen.map Choice2Of2 au.Gen ]
    Shrinker = Shrink.shrinkChoice at.Shrinker au.Shrinker
    PrettyPrinter = Pretty.prettyAny
  }

  // port from FsCheck

(*--------------------------------------------------------------------------*\
**  FsCheck                                                                 **
**  Copyright (c) 2008-2015 Kurt Schelfthout and contributors.              **  
**  All rights reserved.                                                    **
**  https://github.com/kurtschelfthout/FsCheck                              **
**                                                                          **
**  This software is released under the terms of the Revised BSD License.   **
**  See the file License.txt for the full text.                             **
\*--------------------------------------------------------------------------*)

  let private fraction (a:int) (b:int) (c:int) =
    double a + double b / (abs (double c) + 1.0)

  let float = {
    Gen =
      Gen.frequency [
        (6, gen {
          let! a = int
          let! b = int
          let! c = int
          return fraction a b c
        })
        (1, Gen.elements [ Double.NaN; Double.NegativeInfinity; Double.PositiveInfinity])
        (1, Gen.elements [ Double.MaxValue; Double.MinValue; Double.Epsilon])]
    Shrinker = Shrink.shrinkAny
    PrettyPrinter = Pretty.prettyAny
  }
