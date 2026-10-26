# Responsiveness verification

There are two different measurements: conversion-engine time and the complete
keypress-to-editor result. Measure them separately so a fast dictionary lookup
does not hide slow selection discovery or clipboard delivery.

## Automated engine benchmark

```powershell
./tools/benchmark.ps1 -Check
```

The command compiles a noninteractive runner and copies factory configuration and
data into a new directory beneath `out/benchmarks`. It never starts resident
hooks, opens an editor, touches the clipboard, reads installed settings or builds
an installer. Python is not required.

The 17 cases cover words, English protection, compounds, spelling choices, letters,
short triggers, single verses, paired verse ranges, selections, italic decisions
and plain/ITRANS/Devanagari/Bengali copied conversion. Each case records its first
request separately, warms up, then measures 301 calls by default. The report gives
median, p95, p99 and maximum in milliseconds; p95 uses the nearest-rank method.
Engine initialization and approximate managed-memory growth are separate fields.
Initialization occurs in a fresh process, but the OS file cache is not cleared.

`tests/benchmarks/budgets.json` sets the warm engine p95 budget to 150 ms. `-Check`
fails if any case exceeds it. Without `-Check`, budget misses are reported for
investigation; compilation and correctness errors always fail. CI uses 101 warm
samples per case to limit duration. Reports are retained as CI artifacts.

This is a ceiling, not a claim that every conversion takes 150 ms or that a native
editor meets that target. Compare reports from similar machines and workloads;
inspect first-call and maximum values as well as the percentile. Keep a previous
report if you need to investigate a performance regression. The command creates
a fresh run directory and never overwrites an earlier report.

## Shared editor contracts

```powershell
./test.ps1 -Suite SelectionTests
```

The existing selection suite additionally runs the same nine scenarios through
six capability profiles: Word, browser plain text, browser rich text, InDesign
text, InDesign search fields and Excel cell editing. Profiles vary direct,
delayed, ignored and keyboard-first selection behavior. Every profile receives
the same spelling, preceding-text, focus-loss, lost-selection, timeout, repeated
cycle and original-spelling checks. Both Unicode and Balarama output are covered.

The matrix deliberately uses unambiguous spellings for exact-output assertions.
The separate `raja` cycling checks allow the dictionary's ranked choices instead
of assuming which valid spelling must rank first. No preferred spelling is
written to a user's configuration.

These fixtures exercise production guards and conversion logic with simulated
editor surfaces. They do not launch native applications or prove a particular
version of an application implements its accessibility interface correctly.

## Native smoke test protocol

Use a separate disposable document or field in each application. Preserve the
currently installed settings for a realistic test; record the app, font, encoding,
caret location, paste method and whether Track Changes is enabled when a failure
occurs. Never run these checks in a document that contains working material.

1. Type `Earlier untouched prasada`, then tap the whole-word key once. Only the
   final word should convert. Repeat as the first word and in a later paragraph.
2. Type `raja` and cycle at least six times. Check that cycling continues and
   Backspace restores the original word. Repeat inside existing and pasted text.
3. Select `Earlier untouched prasada and Krsna. 24` and convert. English and the
   number must remain unchanged. Check that the selection stays selected, undo
   restores it once, and subsequent English typing does not inherit italics.
4. Type a supported letter, press the letter key immediately, then rapidly cycle.
   Change to a different letter and repeat. Check for literal backticks, spaces,
   missing input and stuck modifiers.
5. Convert, then immediately type ` next`. Check that every queued keystroke is
   preserved in order. Change focus during another attempt: no input may be
   replayed into a different field or leave a partial conversion.
6. In Word, also test ScaGoudy, mixed Unicode fallback, Paste Options, later
   paragraphs and Track Changes. In InDesign, cover text frames and Text/GREP
   search fields without executing Find/Change. In Excel, use a visible cell-edit
   caret and keep formulas untouched. In browsers, test both plain and rich text.

Record timing separately for first use and subsequent conversions. A full native
latency test must include target discovery, verification, formatting, clipboard
ownership and queued delivery. Do not shorten verification waits solely to make
an engine-only benchmark faster.
