using System;
using System.Collections;
using System.Collections.Generic;

namespace PoshDotNet.Cmdlets.Analyzers.Model;

/// <summary>
/// Represents an immutable array that equals another array whose elements are equal and in the same order.
/// </summary>
/// <remarks>
/// <para>
/// A model that holds this type stays value-equatable, so an incremental generator can tell an unchanged model from a
/// changed one.
/// </para>
/// <para>
/// The default value is an empty array.
/// </para>
/// </remarks>
/// <typeparam name="T">The element type, which compares by value.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
	where T : IEquatable<T>
{
	/// <summary>
	/// The elements, or <see langword="null"/> for an empty array.
	/// </summary>
	private readonly T[]? _items;

	/// <summary>
	/// Initializes a new <see cref="EquatableArray{T}"/> instance that holds a copy of the specified elements.
	/// </summary>
	/// <param name="items">The elements to copy, in order.</param>
	public EquatableArray(IEnumerable<T> items)
	{
		T[] copy = [.. items];
		_items = copy.Length == 0 ? null : copy;
	}

	/// <summary>
	/// Gets the number of elements.
	/// </summary>
	/// <value>
	/// The number of elements in the array.
	/// </value>
	public int Count => _items?.Length ?? 0;

	/// <summary>
	/// Gets the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is negative, or not less than <see cref="Count"/>.</exception>
	/// <value>
	/// The element at <paramref name="index"/>.
	/// </value>
	public T this[int index] => (_items ?? [])[index];

	/// <summary>
	/// Determines whether two arrays hold equal elements in the same order.
	/// </summary>
	/// <param name="left">The first array.</param>
	/// <param name="right">The second array.</param>
	/// <returns><see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Determines whether two arrays differ in length or in any element.
	/// </summary>
	/// <param name="left">The first array.</param>
	/// <param name="right">The second array.</param>
	/// <returns><see langword="true"/> if the arrays differ; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
	{
		return !left.Equals(right);
	}

	/// <summary>
	/// Determines whether this array holds the same elements as another, in the same order.
	/// </summary>
	/// <param name="other">The array to compare with.</param>
	/// <returns><see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.</returns>
	public bool Equals(EquatableArray<T> other)
	{
		if (this.Count != other.Count)
		{
			return false;
		}

		for (int i = 0; i < this.Count; i++)
		{
			if (!EqualityComparer<T>.Default.Equals(_items![i], other._items![i]))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Determines whether this array equals the specified object.
	/// </summary>
	/// <param name="obj">The object to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="obj"/> is an <see cref="EquatableArray{T}"/> with the same elements;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public override bool Equals(object? obj)
	{
		return obj is EquatableArray<T> other && this.Equals(other);
	}

	/// <summary>
	/// Returns a hash code that combines the hash codes of the elements.
	/// </summary>
	/// <returns>The hash code.</returns>
	public override int GetHashCode()
	{
		int hash = 17;
		for (int i = 0; i < this.Count; i++)
		{
			hash = unchecked((hash * 31) + EqualityComparer<T>.Default.GetHashCode(_items![i]!));
		}

		return hash;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the elements.
	/// </summary>
	/// <returns>An enumerator over the elements, in order.</returns>
	public IEnumerator<T> GetEnumerator()
	{
		return ((IEnumerable<T>)(_items ?? [])).GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the elements.
	/// </summary>
	/// <returns>An enumerator over the elements, in order.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}
