using System;
using System.Drawing;
using System.Linq;

namespace Keystone.Extensions
{
    public static class ArrayExtensions
    {
        public static byte[] StringToByteArray(string str, int totalFixedLength)
        {
            if (str == null) throw new ArgumentNullException();
            if (str.Length > totalFixedLength) throw new ArgumentOutOfRangeException();

            return System.Text.Encoding.ASCII.GetBytes(str.PadRight(totalFixedLength, '\0'));

        }

        #region Arrays
        public static int ArrayCount<T>(this T[] array)
        {
            if (array == null)
                return 0;
            else
                return array.Length;
        }

        public static int IndexOf<T>(this T[] array, Predicate<T> match)
        {
            if (array == null || array.Length == 0) return -1;
            for (int i = 0; i < array.Length; i++)
                if (match (array[i])) return i;

            return -1;
        }

        public static int IndexOf<T>(this T[] array, T element)
        {
            if (array == null || array.Length == 0) return -1;
            for (int i = 0; i < array.Length; i++)
                if (array[i].Equals(element)) return i;

            return -1;
        }

        public static bool ArrayContains<T>(this T[] array, Predicate<T> match)
        {
            if (array == null || array.Length == 0) return false;
            for (int i = 0; i < array.Length; i++)
                if (match(array[i])) return true;

            return false;
        }

        public static bool ArrayContains<T>(this T[] array, T element)
        {
            if (array == null || array.Length == 0) return false;
            for (int i = 0; i < array.Length; i++)
                if (array[i].Equals(element)) return true;

            return false;
        }

        /// <summary>
        /// Grow an array by one element and append new element there.
        /// </summary>
        /// <remarks>
        /// This method RETURNS the new array and does NOT change the array passed in.
        /// </remarks>
        public static T[] ArrayAppend<T>(this T[] array, T element)
        {
            T[] tmp;
            if (array == null)
            {
                tmp = new T[1];
                tmp[0] = element;
            }
            else
            {
                tmp = new T[array.Length + 1];
                array.CopyTo(tmp, 0);
                tmp[array.Length] = element;
            }

            return tmp;
        }

        /// <summary>
        /// Grow an array by one element and append new element there.
        /// </summary>
        /// <remarks>
        /// This method RETURNS the new array and does NOT change the array passed in.
        /// </remarks>
        public static T[] ArrayAppendRange<T>(this T[] array, T[] elements)
        {
          
            T[] tmp;
            if (array == null)
            {
                tmp = new T[elements.Length];
                for (int i = 0; i < elements.Length; i++)
                    tmp[i] = elements[i];
            }
            else
            {
                tmp = new T[array.Length + elements.Length];
                array.CopyTo(tmp, 0);
                elements.CopyTo(tmp, array.Length);
            }

            return tmp;
        }

        public static T[] ArrayUnion<T>(this T[] array, T[] elements)
        {
            T[] tmp;
            if (array == null)
            {
                tmp = new T[elements.Length];
                for (int i = 0; i < elements.Length; i++)
                    tmp[i] = elements[i];
            }
            else
                tmp = array.Union(elements).ToArray ();

            return tmp;
        }

        public static T[] ArraySlice<T>(this T[] array, uint count)
        {

            if (array == null || count == 0)
                return null;

            if (count > array.Length) throw new ArgumentOutOfRangeException();

            T[] tmp = new T[count];
            Array.Copy(array, 0, tmp, 0, count);
            return tmp;
        }

        public static T[] ArrayInsertAt<T>(this T[] array, T element, uint index)
        {
        	if (array == null) throw new ArgumentNullException();
        	if (index > array.Length - 1) throw new ArgumentOutOfRangeException ();
        	
        	if (index == array.Length - 1)
        		return ArrayAppend (array, element);
        	
        	// create new array that is +1 size greater than existing
        	int size = array.Length + 1;
        	T[] result = new T[size];
        	
        	if (index == 0)
        	{
        		// one array copy needed
        		result[0] = element;
        		Array.Copy(array, 0, result, 1, array.Length);    		
        	}
        	else 
        	{
        		throw new NotImplementedException("need to test this path");
        		// copy before
        		Array.Copy(array, 0, result, 0, index);    		
        		
        		// assign new element
        		result[index] = element;
        		
        		// copy after
        		Array.Copy(array, index, result, index + 1, array.Length - (index + 1));
        	}
        	
            return result;
        }

        /// <summary>
        /// Searches for an element and removes it if found and returns the index where it was.
        /// Returns -1 if not found
        /// </summary>
        public static T[] ArrayRemove<T>(this T[] array, T element)
        {
            int loc = 0;
            bool found = false;
            foreach (T t in array)
            {
                if (t.Equals(element))
                {
                    found = true;
                    break;
                }
                loc++;
            }
            if (!found)
            {
                return array;
            }

            System.Collections.Generic.List<T> tmp = new System.Collections.Generic.List<T>();
            for (int i = 0; i < array.Length; i++)
            {
                if (i != loc) tmp.Add(array[i]);
            }
            if (tmp.Count > 0)
            {
                array = tmp.ToArray();
            }
            else
            {
                array = null;
            }

            return array;
        }


        public static T[] ArrayRemoveAt<T>(this T[] array, int index)
        {
            if (array == null) throw new ArgumentNullException();
            if (index > array.Length - 1) throw new ArgumentOutOfRangeException();

            T[] result = null;

            if (array.Length == 1)
            {
                System.Diagnostics.Debug.Assert(index == 0);
                array = result; // array is now empty
                return array;
            }

            result = new T[array.Length - 1];

            if (index == array.Length - 1)
            {
                // if it's the last element, return a new array that is one element shorter
                Array.Copy(array, 0, result, 0, array.Length - 1);
                array = result;
                return array;
            }

            // if we're still here, now we need to actually shift elements over and then
            // return with one less element
            int lastIndex = array.Length - 1;
            Array.Copy(array, 0, result, 0, index);
            Array.Copy(array, index + 1, result, index, lastIndex - index);
            array = result;

            return array;
        }

        public static T[] RemoveAt<T>(this T[] source, int index)
        {
            T[] dest = new T[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, dest, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        }

        #endregion

        /// <summary>
        /// Rotates a square array clockwise by 90 degrees in place without
        /// allocating another array for the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">Arrays must be square.  </param>
        /// <returns></returns>
        public static T[,] Rotate90InPlace<T>(this T[,] array)
        {
            // must be a square array to rotate in place obviously because rotating in place
            // within memory means by definition that the dimensions of the array are fixed
            // and if width and height are not equal then this obviously results in a
            // subscript out of range.
            System.Diagnostics.Debug.Assert(array.GetLength(0) == array.GetLength(1));
            int n = array.GetLength(0);
            int f = (int)Math.Floor(n * .5d);
            int c = (int)Math.Ceiling(n * .5d);

            for (int x = 0; x < f; x++)
                for (int y = 0; y < c; y++)
                {
                    T temp = array[x, y];
                    array[x, y] = array[y, n - 1 - x];
                    array[y, n - 1 - x] = array[n - 1 - x, n - 1 - y];
                    array[n - 1 - x, n - 1 - y] = array[n - 1 - y, x];
                    array[n - 1 - y, x] = temp;
                }

            return array;
        }


        public static T[,] Rotate270<T>(this T[,] array)
        {
            int width = array.GetLength(0);
            int height = array.GetLength(1);

            // for 90, height and width dimensions are reversed
            T[,] result = new T[height, width];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    result[height - j - 1, i] = array[i, j];

            return result;
        }

        public static T[,] Rotate180<T>(this T[,] array)
        {
            int width = array.GetLength(0);
            int height = array.GetLength(1);

            // width is the same since 180 rotation only turns image upside down
            T[,] result = new T[width, height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    result[width - i - 1, height - j - 1] = array[i, j];

            return result;
        }

        public static T[,] Rotate90<T>(this T[,] array)
        {
            int width = array.GetLength(0);
            int height = array.GetLength(1);

            // for 270, height and width dimensions are reversed
            T[,] result = new T[height, width];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    result[j, width - i - 1] = array[i, j];

            return result;
        }

        // http://support.microsoft.com/kb/80406
        // http://www.devx.com/vb2themax/Tip/19360
       public static T[,] RotateTheta<T>(this T[,] array, double thetaRadians)
       {
           // source footprint center
           int sourceCenterX = array.GetLength(0) / 2;
           int sourceCenterY = array.GetLength(1) / 2;

           // compute large enough destination array to hold source when rotated at any angle
           int lengthA = array.GetLength(0);
           int lengthB = array.GetLength(1);
           
           // pythagorean therom to get max possible radius 
           int n = (int)Math.Sqrt(lengthA * lengthA + lengthB * lengthB);

           // destination center uses center of our max possible square
           int destCenterX = n / 2; 
           int destCenterY = n / 2;
           
           T[,] result = new T[n, n];
           n = n / 2 - 1;
           
           // For half of x axis and half of y axis coordinates (these two positive halfs results in just 1 quadrant worth of coordinates covered but we can deduce the other 3 quadrants)
           for (int destX = 0; destX < n; destX++)
           {
               for (int destY = 0; destY < n; destY++)
               {
                   // Compute polar coordinate of dest.
                   // Start by computing Angle of source center to current dest pixel.
                   double a = Math.Atan2(destY, destX);  
   
                   // Radius from source center to current dest pixel.
                   // use pythagorean therom to get radius (c2 = a2 + b2) of current footprint coordinate
                   int r = (int)Math.Sqrt(destX * destX + destY * destY);

                   // Compute rotated position of this coordinate.
                   int sourceX = (int)(r * Math.Cos(a + thetaRadians));
                   int sourceY = (int)(r * Math.Sin(a + thetaRadians));

                   // Copy pixels, 4 quadrants at once.
                   T source0 = array.GetValueAt(sourceCenterX + sourceX, sourceCenterY + sourceY);
                   T source1 = array.GetValueAt(sourceCenterX - sourceX, sourceCenterY - sourceY);
                   T source2 = array.GetValueAt(sourceCenterX + sourceY, sourceCenterY - sourceX);
                   T source3 = array.GetValueAt(sourceCenterX - sourceY, sourceCenterY + sourceX);

                   // assign the source to the destination location
                   result[destCenterX + destX, destCenterY + destY] = source0;
                   result[destCenterX - destX, destCenterY - destY] = source1;
                   result[destCenterX + destY, destCenterY - destX] = source2;
                   result[destCenterX - destY, destCenterY + destX] = source3;
              }
           }
           return result;
        }

       private static T GetValueAt<T>(this T[,] array, int x, int y)
       {
           int width = array.GetLength(0);
           int height = array.GetLength(1);

           if (x < 0 || y < 0 || x >= width || y >= height) return default(T);
           return array[x, y];
       }

        // TODO: it seems to me that the rotated image is 33% bigger than the widest dimension
        //       so destination must be that much bigger and square
        // TODO: someone mentioned that you can also just build a 45 degree version of the pattern
        //       for use in those cases... we'll keep that in mind as some last option
        // http://rajendrauppal.blogspot.com/2011/12/rotating-2d-array-of-integers-matrix-by.html
        // http://stackoverflow.com/questions/3279148/rotate-2d-array-by-45-degrees
        public static T[,] Rotate45<T>(this T[,] array)
        {
            int maxDimension = Math.Max(array.GetLength(0), array.GetLength(1));
            T[,] result = new T[maxDimension, maxDimension];

            Point center = new Point((result.GetLength(0) - 1) / 2, (result.GetLength(1) - 1) / 2);
            Point center2 = new Point((array.GetLength(0) - 1) / 2, (array.GetLength(1) - 1) / 2);
            for (int r = 0; r <= (maxDimension - 1) / 2; r++)
            {
                for (int i = 0; i <= r * 8; i++)
                {
                    Point source = RoundIndexToPoint(i, r);
                    Point target = RoundIndexToPoint(i + r, r);

                    if (!(center2.X + source.X < 0 || center2.Y + source.Y < 0 || center2.X + source.X >= array.GetLength(0) || center2.Y + source.Y >= array.GetLength(1)))
                        result[center.X + target.X, center.Y + target.Y] = array[center2.X + source.X, center2.Y + source.Y];
                }
            }
            return result;
        }

        // http://stackoverflow.com/questions/3279148/rotate-2d-array-by-45-degrees
        public static Point RoundIndexToPoint(int index, int radius)
        {
            if (radius == 0)
                return new Point(0, 0);
            Point result = new Point(-radius, -radius);

            while (index < 0) index += radius * 8;
            index = index % (radius * 8);

            int edgeLen = radius * 2;

            if (index < edgeLen)
            {
                result.X += index;
            }
            else if ((index -= edgeLen) < edgeLen)
            {
                result.X = radius;
                result.Y += index;
            }
            else if ((index -= edgeLen) < edgeLen)
            {
                result.X = radius - index;
                result.Y = radius;
            }
            else if ((index -= edgeLen) < edgeLen)
            {
                result.Y = radius - index;
            }

            return result;
        }
    } 
}
