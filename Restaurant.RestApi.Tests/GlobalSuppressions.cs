/* Copyright (c) Mark Seemann 2020. All rights reserved. */
// 이 파일은 프로젝트에서 코드 분석기의 SuppressMessage 속성을
// 유지 관리하는데 사용됩니다. 
// 프로젝트 수준에서 메시지를 발생하지 않도록 하는 것은 대상을 지정하지 않거나 
// 특정 대상 혹은 특정 네임스페이스, 형식, 멤버등으로 범위를 지정할 수 있습니다.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = @"This is a test library, not a generally reusable library.
The consumers are known.")]
