// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices {

    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit {

    }

    /// <summary>
    /// Used to indicate to the compiler that the <c>.locals init</c> flag should not be set in method headers.
    /// </summary>
    /// <remarks>Internal copy of the .NET 5 attribute.</remarks>
    [AttributeUsage(
        AttributeTargets.Module      |
        AttributeTargets.Class       |
        AttributeTargets.Struct      |
        AttributeTargets.Interface   |
        AttributeTargets.Constructor |
        AttributeTargets.Method      |
        AttributeTargets.Property    |
        AttributeTargets.Event,
        Inherited = false)]
    internal sealed class SkipLocalsInitAttribute: Attribute {

    }

}