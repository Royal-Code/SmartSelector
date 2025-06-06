﻿namespace RoyalCode.SmartSelector;

/// <summary>
/// <para>
///     This attribute is used to mark that one class is selectable from another, 
///     and the code generator must create the necessary functions.
///     <br />
///     It will generate a select expression, a From method for conversion, and extension methods for select.
/// </para>
/// </summary>
/// <typeparam name="TFrom">
///     The type to convert from.
/// </typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AutoSelectAttribute<TFrom> : Attribute { }
