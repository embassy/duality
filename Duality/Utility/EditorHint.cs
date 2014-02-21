﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Drawing;

namespace Duality.Editor
{
	/// <summary>
	/// Some general flags for Type members that indicate preferred editor behaviour.
	/// </summary>
	[Flags]
	public enum MemberFlags
	{
		/// <summary>
		/// No flags set.
		/// </summary>
		None			= 0x00,

		/// <summary>
		/// When editing the Properties or Fields value, a final set operation is requested to finish editing.
		/// </summary>
		ForceWriteback	= 0x01,
		/// <summary>
		/// The member is considered invisible. Will override visibility rules derived from reflection.
		/// </summary>
		Invisible		= 0x02,
		/// <summary>
		/// The member is considered read-only, even if writing is possible via reflection.
		/// </summary>
		ReadOnly		= 0x04,
		/// <summary>
		/// Indicates that editing the member may have an effect on any other member of the current object.
		/// </summary>
		AffectsOthers	= 0x08,
		/// <summary>
		/// The member is considered visible. Will override visibility rules derived from reflection.
		/// </summary>
		Visible			= 0x10
	}

	/// <summary>
	/// An attribute that provides information about a Types or Members preferred editor behaviour.
	/// </summary>
	public abstract class EditorHintAttribute : Attribute {}

	/// <summary>
	/// Provides general information about a members preferred editor behaviour.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
	public class EditorHintFlagsAttribute : EditorHintAttribute
	{
		private	MemberFlags	flags;
		/// <summary>
		/// [GET] Flags that indicate the members general behaviour
		/// </summary>
		public MemberFlags Flags
		{
			get { return this.flags; }
		}
		public EditorHintFlagsAttribute(MemberFlags flags)
		{
			this.flags = flags;
		}
	}

	/// <summary>
	/// Provides information about a numerical members allowed value range.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public class EditorHintRangeAttribute : EditorHintAttribute
	{
		private	decimal	min;
		private	decimal	max;
		/// <summary>
		/// [GET] The members minimum value
		/// </summary>
		public decimal Min
		{
			get { return this.min; }
		}
		/// <summary>
		/// [GET] The members maximum value
		/// </summary>
		public decimal Max
		{
			get { return this.max; }
		}
		public EditorHintRangeAttribute(int min, int max)
		{
			this.min = min;
			this.max = max;
		}
		public EditorHintRangeAttribute(float min, float max)
		{
			this.min = MathF.SafeToDecimal(min);
			this.max = MathF.SafeToDecimal(max);
		}
	}

	/// <summary>
	/// Provides information about a numerical members value increment.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public class EditorHintIncrementAttribute : EditorHintAttribute
	{
		private	decimal	inc;
		/// <summary>
		/// [GET] The members value increment.
		/// </summary>
		public decimal Increment
		{
			get { return this.inc; }
		}
		public EditorHintIncrementAttribute(int inc)
		{
			this.inc = inc;
		}
		public EditorHintIncrementAttribute(float inc)
		{
			this.inc = (decimal)inc;
		}
	}

	/// <summary>
	/// Provides information about a numerical members decimal accuracy
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public class EditorHintDecimalPlacesAttribute : EditorHintAttribute
	{
		private	int places;
		/// <summary>
		/// [GET] The preferred number of displayed decimal places
		/// </summary>
		public int Places
		{
			get { return this.places; }
		}
		public EditorHintDecimalPlacesAttribute(int places)
		{
			this.places = places;
		}
	}

	/// <summary>
	/// Provides information about a Types editor category.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
	public class EditorHintCategoryAttribute : EditorHintAttribute
	{
		private	string		category		= null;
		private	string[]	categoryTree	= null;

		/// <summary>
		/// [GET] The preferred category tree to fit this Type in, split into hierarchial tokens.
		/// </summary>
		public string[] CategoryTree
		{
			get { return this.categoryTree; }
		}
		/// <summary>
		/// [GET] The preferred category tree to fit this Type in.
		/// </summary>
		public string Category
		{
			get { return this.category; }
		}

		public EditorHintCategoryAttribute(Type resourceClass, string propertyName)
		{
			PropertyInfo resourceProperty = resourceClass.GetProperty(propertyName, ReflectionHelper.BindStaticAll);
			if (resourceProperty != null && resourceProperty.PropertyType == typeof(string))
			{
				this.category = (string)resourceProperty.GetValue(null, null);
			}
			else
			{
				this.category = propertyName;
			}
			this.UpdateCategoryTree();
		}
		public EditorHintCategoryAttribute(string category)
		{
			this.category = category;
			this.UpdateCategoryTree();
		}
		private void UpdateCategoryTree()
		{
			if (!string.IsNullOrWhiteSpace(this.category))
			{
				this.categoryTree = this.category.Split(
					new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, 
					StringSplitOptions.RemoveEmptyEntries);
			}
			else
			{
				this.categoryTree = new string[0];
			}
		}
	}

	/// <summary>
	/// Provides an icon or image that can be used to represent the given Type within the editor.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
	public class EditorHintImageAttribute : EditorHintAttribute
	{
		private	Image	iconImage	= null;

		/// <summary>
		/// [GET] The icon image that will be used to represent this Type.
		/// </summary>
		public Image IconImage
		{
			get { return this.iconImage; }
		}

		public EditorHintImageAttribute(Type resourceClass, string propertyName)
		{
			PropertyInfo resourceProperty = resourceClass.GetProperty(propertyName, ReflectionHelper.BindStaticAll);
			if (resourceProperty != null && typeof(Image).IsAssignableFrom(resourceProperty.PropertyType))
			{
				this.iconImage = resourceProperty.GetValue(null, null) as Image;
			}
			else
			{
				this.iconImage = null;
			}
		}
	}

	public static class ExtMethodsMemberInfoEditorHint
	{
		/// <summary>
		/// Returns the category tree which this Type prefers to be in.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string[] GetEditorCategory(this Type type)
		{
			string[] tree = null;
			foreach (var attrib in type.GetEditorHints<EditorHintCategoryAttribute>())
			{
				tree = attrib.CategoryTree;
				if (tree != null) break;
			}
			if (tree == null) tree = type.Namespace.Split('.');
			return tree;
		}
		/// <summary>
		/// Return the preferred icon image representation of the specified Type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static Image GetEditorImage(this Type type)
		{
			var w = System.Diagnostics.Stopwatch.StartNew();
			Image image = null;
			foreach (var attrib in type.GetEditorHints<EditorHintImageAttribute>())
			{
				image = attrib.IconImage;
				if (image != null) break;
			}
			w.Stop();
			Console.WriteLine("{0}: {1:F}", type.Name, w.Elapsed.TotalMilliseconds);
			return image;
		}

		/// <summary>
		/// Retrieves the first <see cref="EditorHintAttribute"/> of the specified Type that is attached to the specified Member.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="info"></param>
		/// <returns></returns>
		public static T GetEditorHint<T>(this MemberInfo info) where T : EditorHintAttribute
		{
			return info.GetCustomAttributes<T>().FirstOrDefault();
		}
		/// <summary>
		/// Retrieves the first <see cref="EditorHintAttribute"/> of the specified Type that is attached to the specified Member.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="info"></param>
		/// <param name="hintOverride">If set, the specified collection of available hints will be used to override the Members own attributes.</param>
		/// <returns></returns>
		public static T GetEditorHint<T>(this MemberInfo info, IEnumerable<EditorHintAttribute> hintOverride) where T : EditorHintAttribute
		{
			T attrib = null;
			if (hintOverride != null) attrib = hintOverride.OfType<T>().FirstOrDefault();
			if (attrib == null && info != null) attrib = info.GetEditorHint<T>();
			return attrib;
		}
		/// <summary>
		/// Retrieves all <see cref="EditorHintAttribute"/> instances of the specified Type that are attached to the specified Member.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="info"></param>
		/// <returns></returns>
		public static IEnumerable<T> GetEditorHints<T>(this MemberInfo info) where T : EditorHintAttribute
		{
			return info.GetCustomAttributes<T>();
		}
		/// <summary>
		/// Retrieves all <see cref="EditorHintAttribute"/> instances of the specified Type that are attached to the specified Member.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="info"></param>
		/// <param name="hintOverride">If set, the specified collection of available hints will be used to override the Members own attributes.</param>
		/// <returns></returns>
		public static IEnumerable<T> GetEditorHints<T>(this MemberInfo info, IEnumerable<EditorHintAttribute> hintOverride) where T : EditorHintAttribute
		{
			if (info == null) return null;

			IEnumerable<EditorHintAttribute> infoHints = info.GetEditorHints<T>();
			if (hintOverride == null) return infoHints.OfType<T>();
			if (infoHints == null) return hintOverride.OfType<T>();

			return infoHints.Where(h => !hintOverride.Any(o => o.GetType().IsInstanceOfType(h))).OfType<T>();
		}
	}
}