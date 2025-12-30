using System;
using System.ComponentModel;
using System.Diagnostics;


namespace Keystone.Types
{
    [TypeConverter(typeof(Keystone.TypeConverters.ColorConverter))]
    public struct Color
    {
        public float r, g, b, a;

//        public Color(int argb) 
//            : this((byte)((argb) & 0xFF), 
//                    (byte)((argb >> 8) & 0xFF), 
//                    (byte)((argb >> 16) & 0xFF),
//                    (byte)((argb >> 24) & 0xFF))
//        {
//        }

		// i'm pretty sure this is correct whereas above is passing the parameters to this() ctor in a, r, g, b instead of r, g, b, a as that ctor expects
        public Color(int argb) 
            : this( 
                    (byte)((argb >> 16) & 0xFF),
                    (byte)((argb >> 8) & 0xFF), 
                   (byte)((argb) & 0xFF),
                   (byte)((argb >> 24) & 0xFF))
        {
        }

        public Color (byte red, byte green, byte blue, byte alpha) 
            : this (red / 255f, green / 255f, blue / 255f, alpha / 255f)
        {
        }

        //public Color(string delimitedString)
        //{
        //    Color parse = Color.Parse(delimitedString);
        //    this.r = parse.r;
        //    this.g = parse.g;
        //    this.b = parse.b;
        //    this.a = parse.a;
        //}

        /// <summary>
        /// values should all be between 0 and 1f
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="alpha"></param>
        public Color(float red, float green, float blue, float alpha)
        {
            Trace.Assert(red <= 1.0f);
            Trace.Assert(green <= 1.0f);
            Trace.Assert(blue <= 1.0f);
            Trace.Assert(alpha <= 1.0f);
            r = red;
            g = green;
            b = blue;
            a = alpha;
        }

        public static Color Scale (float r, float g, float b, float a, float scale)
        {
        	Color result;
        	result.r = r * scale;
        	result.g = g * scale;
        	result.b = b * scale;
        	result.a = a * scale;
        	return result;
        	
        }
        

        public static Color Black { get { return new Color (0, 0, 0, 255); } }
        public static Color White { get { return new Color(255, 255, 255, 255); } }
        public static Color Gray {  get { return new Color(128, 128, 128, 255); } }
        public static Color Red { get { return new Color(255, 0, 0, 255); } }
        public static Color Green { get { return new Color(0, 255, 0, 255); } }
        public static Color Blue { get { return new Color(0, 0, 255, 255); } }

        public static Color Cyan { get { return new Color(0, 255, 255, 255); } }
        public static Color Magenta { get { return new Color(255, 0, 255, 255); } }
        public static Color Yellow { get { return new Color(255, 255, 0, 255); } }

        public static Color Grey { get { return new Color(127, 127, 127, 255); } }

        public static Color LightYellow { get { return new Color(255, 255, 224, 255); } }
        public static Color RoyalBlue { get { return new Color(65, 105, 225, 255); } }

        public static Color Silver { get { return new Color(192, 192, 192, 255); } }
        public static Color Gold { get { return new Color(255, 215, 0, 255); } }
        public static Color Bronze { get { return new Color(205, 127, 50, 255); } }
        public static Color Copper { get { return new Color(184, 115, 51, 255); } }
        public static Color Iron { get{ return new Color(203, 205, 205, 255); } }

        public static bool operator ==(Color c1, Color c2)
        {
            return c1.r == c2.r &&
                c1.g == c2.g &&
                c1.b == c2.b &&
                c1.a == c2.a;
        }

        public static bool operator !=(Color c1, Color c2)
        {
            return (c1 == c2) == false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if ((obj is Color) == false) 
                return false;

            return (this == (Color)obj);
        }

        public static int RGBA (float r, float g, float b, float a)
        {
        	Color c = new Color (r, g, b, a);
        	return c.ToInt32 ();
        }
        
        /// <summary>
        /// ToInt32() is same as RGBA(float r, float g, float b, float a) which is also same as System.Drawing.Color.ToArgb
        /// The byte-ordering of the 32-bit ARGB value is AARRGGBB. 
        /// The most significant byte (MSB), represented by AA, is the alpha component value. 
        /// The second, third, and fourth bytes, represented by RR, GG, and BB, respectively, 
        /// are the color components red, green, and blue.
        /// </summary>
        /// <returns></returns>
        public int ToInt32()
        {
            return RGBA(r, g, b, a);
        }

        /// <summary>
        /// RGBA is same as System.Drawing.Color.ToArgb
        /// The byte-ordering of the 32-bit ARGB value is AARRGGBB. 
        /// The most significant byte (MSB), represented by AA, is the alpha component value. 
        /// The second, third, and fourth bytes, represented by RR, GG, and BB, respectively, 
        /// are the color components red, green, and blue.
        /// </summary>
        /// <returns></returns>
        public static int RGBA(float r, float g, float b, float a)
        {
            int A = (int)(255 * a);
            int R = (int)(255 * r);
            int G = (int)(255 * g);
            int B = (int)(255 * b);

            A = A << 24;
            R = R << 16;
            G = G << 8;

            return A | R | G | B;
        }


            //// To integer
            //int iCol = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

            //// From integer
            //Color color = Color.FromArgb((byte)(iCol >> 24),
            //                             (byte)(iCol >> 16),
            //                             (byte)(iCol >> 8),
            //                             (byte)(iCol));
        }



        public static Color Parse(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars);

            float r = float.Parse(values[0]);
            float g = float.Parse(values[1]); 
            float b = float.Parse(values[2]); 
            float a = float.Parse(values[3]); 
            return new Color(r, g, b, a);
        }

        public override string ToString()
        {
            string delimiter = keymath.ParseHelper.English.XMLAttributeDelimiter;
            return string.Format("{0}{1}{2}{3}{4}{5}{6}", r, delimiter,
                                                    g, delimiter,
                                                    b, delimiter,
                                                    a);
        }

    }
}
