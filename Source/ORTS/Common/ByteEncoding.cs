// COPYRIGHT 2013 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.

using System.Text;

namespace ORTS.Common
{
	/// <summary>
	/// A basic encoding which maps bytes 0-255 to Unicode characters 0-255.
	/// </summary>
	public class ByteEncoding : Encoding
	{
		public static Encoding Encoding = new ByteEncoding();

		public ByteEncoding() {
		}

		/// <summary>
		/// Calculates the number of bytes produced by encoding a set of characters from the specified character array.
		/// </summary>
		/// <param name="chars">The character array containing the set of characters to encode.</param>
		/// <param name="index">The index of the first character to encode.</param>
		/// <param name="count">The number of characters to encode.</param>
		/// <returns>The number of bytes produced by encoding the specified characters.</returns>
		public override int GetByteCount(char[] chars, int index, int count) {
			return count;
		}

		/// <summary>
		/// Encodes a set of characters from the specified character array into the specified byte array.
		/// </summary>
		/// <param name="chars">The character array containing the set of characters to encode.</param>
		/// <param name="charIndex">The index of the first character to encode.</param>
		/// <param name="charCount">The number of characters to encode.</param>
		/// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
		/// <param name="byteIndex">The index at which to start writing the resulting sequence of bytes.</param>
		/// <returns>The actual number of bytes written into <paramref name="bytes"/>.</returns>
		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
			for (var i = 0; i < charCount; i++) {
				bytes[i + byteIndex] = (byte)chars[i + charIndex];
			}
			return charCount;
		}

		/// <summary>
		/// Calculates the number of characters produced by decoding a sequence of bytes from the specified byte array.
		/// </summary>
		/// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
		/// <param name="index">The index of the first byte to decode.</param>
		/// <param name="count">The number of bytes to decode.</param>
		/// <returns>The number of characters produced by decoding the specified sequence of bytes.</returns>
		public override int GetCharCount(byte[] bytes, int index, int count) {
			return count;
		}

		/// <summary>
		/// Decodes a sequence of bytes from the specified byte array into the specified character array.
		/// </summary>
		/// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
		/// <param name="byteIndex">The index of the first byte to decode.</param>
		/// <param name="byteCount">The number of bytes to decode.</param>
		/// <param name="chars">The character array to contain the resulting set of characters.</param>
		/// <param name="charIndex">The index at which to start writing the resulting set of characters.</param>
		/// <returns>The actual number of characters written into <paramref name="chars"/>.</returns>
		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
			for (var i = 0; i < byteCount; i++) {
				chars[i + charIndex] = (char)bytes[i + byteIndex];
			}
			return byteCount;
		}

		/// <summary>
		/// Calculates the maximum number of bytes produced by encoding the specified number of characters.
		/// </summary>
		/// <param name="charCount">The number of characters to encode.</param>
		/// <returns>The maximum number of bytes produced by encoding the specified number of characters.</returns>
		public override int GetMaxByteCount(int charCount) {
			return charCount;
		}

		/// <summary>
		/// Calculates the maximum number of characters produced by decoding the specified number of bytes.
		/// </summary>
		/// <param name="byteCount">The number of bytes to decode.</param>
		/// <returns>The maximum number of characters produced by decoding the specified number of bytes.</returns>
		public override int GetMaxCharCount(int byteCount) {
			return byteCount;
		}
	}
}
