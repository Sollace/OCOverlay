using System;
using System.Collections.Generic;
using System.Linq;

namespace OCOverlay {
    public static class Extensions {
        internal static Random rand = new Random();
        private static string characters = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>Returns a random boolean value</summary>
        public static bool NextBoolean(this Random r) {
            return r.NextDouble() == 0.5;
        }
        /// <summary>Returns a number within a specified range. The number may not be whole.</summary>
        /// <param name="min">The inclusive lower bound of the random number returned.</param>
        /// <param name="max">The exclusive upper bound of the random number returned. max must be greater than or equal to min.</param>
        /// <returns>A 32-bit signed integer greater than or equal to min and less than max; that is, the range of return values includes min but not max. If min equals max, min is returned.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">minValue is greater than maxValue.</exception>
        public static float NextFloat(this Random r, int min, int max) {
            return (float)(r.Next(min, max) + r.NextDouble());
        }
        /// <summary>Returns a random character within a specified range of character indexes.</summary>
        /// <param name="min">The inclusive lower bound of the character returned</param>
        /// <param name="max">The exclusive upper bound of the character returned. max must be greater than of equal to min.</param>
        /// <returns>A UTF-16 coded character greater than or equal to min and less than max.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">min is greater than max.</exception>
        public static char NextChar(this Random r, int min, int max) {
            return Char.Parse(Char.ConvertFromUtf32(r.Next(min, max)));
        }
        /// <summary>Returns a random character within a specified range of character indexes.</summary>
        /// <param name="min">The inclusive lower bound of the character returned</param>
        /// <param name="max">The exclusive upper bound of the character returned. max must be greater than of equal to min.</param>
        /// <returns>A UTF-16 coded character greater than or equal to min and less than max.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">min is greater than max.</exception>
        public static char NextChar(this Random r, char min, char max) {
            return r.NextChar((int)min, max);
        }
        /// <summary>Returns a random sequence of characters</summary>
        /// <param name="length">The length of the string returned. Must be greater than or equal to 0</param>
        /// <returns>A sequence of random characters of the given length.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">length is less than zero</exception>
        public static string NextString(this Random r, int length) {
            if (length < 0) throw new ArgumentOutOfRangeException("length is less than zero");
            string result = "";
            while (result.Length < length) result += characters[r.Next(characters.Length)];
            return result;
        }
        /// <summary>
        /// Returns the result of a roll of a dice; a number between 1 inclusive and 7 exclusive
        /// </summary>
        /// <returns>A number betwen i inclusive and 7 exclusive</returns>
        public static int NextDice(this Random r) {
            return r.Next(1, 7);
        }

        /// <summary>Picks a single item based on the current time</summary>
        /// <typeparam name="T">Type of object picked</typeparam>
        /// <returns>KeyValuePair picked from dictionary</returns>
        public static KeyValuePair<P, Q> pickNext<P, Q>(this IDictionary<P, Q> dictionary) {
            return dictionary.ToList().pickNext();
        }
        /// <summary>Picks a single item based on the current time</summary>
        /// <typeparam name="T">Type of object picked</typeparam>
        /// <returns>Item picked</returns>
        public static T pickNext<T>(this IEnumerable<T> collection) {
            return collection.ElementAt(DateTime.Now.Second % collection.Count());
        }
        /// <summary>Picks a single item based on the current time</summary>
        /// <typeparam name="T">Type of object picked</typeparam>
        /// <returns>Item picked</returns>
        public static T pickNext<T>(this Array array) {
            return (T)array.GetValue(DateTime.Now.Second % array.Length);
        }
        /// <summary>Picks a single item based on the current time</summary>
        /// <typeparam name="T">Type of object picked</typeparam>
        /// <returns>Item picked</returns>
        public static T pickNext<T>(this T[] array) {
            return array[DateTime.Now.Second % array.Length];
        }
        /// <summary>Picks a single character based on the current time</summary>
        /// <typeparam name="T">Type of object picked</typeparam>
        /// <returns>Item picked</returns>
        public static char pickNext(this string text) {
            return text.ToCharArray().pickNext();
        }

        /// <summary>Picks a single item</summary>
        /// <typeparam name="T">Type of object picked</typeparam>
        /// <returns>KeyValuePair picked from dictionary</returns>
        public static KeyValuePair<P, Q> pickOne<P, Q>(this IDictionary<P, Q> dictionary) {
            return dictionary.ToList().pickOne();
        }
        /// <summary>Picks a single item</summary>
        /// <typeparam name="T">Type of object picked</typeparam>
        /// <returns>Item picked</returns>
        public static T pickOne<T>(this IEnumerable<T> collection) {
            return collection.ElementAt(rand.Next(0, collection.Count()));
        }
        /// <summary>Picks a single item</summary>
        /// <typeparam name="T">Type of object picked</typeparam>
        /// <returns>Item picked</returns>
        public static T pickOne<T>(this Array array) {
            return (T)array.GetValue(rand.Next(array.Length));
        }
        /// <summary>Picks a single item</summary>
        /// <typeparam name="T">Type of object picked</typeparam>
        /// <returns>Item picked</returns>
        public static T pickOne<T>(this T[] array) {
            return array[rand.Next(0, array.Length)];
        }
        /// <summary>Randomly picks a single character from a string</summary>
        /// <returns>The character picked</returns>
        public static char pickOne(this string text) {
            return text.ToCharArray().pickOne();
        }

        /// <summary>Randomly picks a number of items from a dictionary.</summary>
        /// <typeparam name="P">Type of entry Key</typeparam>
        /// <typeparam name="Q">Type of entry Value</typeparam>
        /// <param name="max">Maximum number of items to pick. By default -1, negative values result in a random number of items being picked</param>
        /// <returns>Result dictionary of picked items</returns>
        public static IDictionary<P, Q> pickSome<P, Q>(this IDictionary<P, Q> dictionary, int max = -1) {
            IEnumerable<P> keys = dictionary.Keys.pickSome(max);
            IDictionary<P, Q> result = new Dictionary<P, Q>();
            foreach (P i in keys) {
                result.Add(i, dictionary[i]);
            }
            return result;
        }
        /// <summary>Randomly picks a number of items from a Enumerable collection.</summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="max">Maximum number of items to pick. By default -1, negative values result in a random number of items being picked</param>
        /// <returns>Result collection of picked items</returns>
        public static IEnumerable<T> pickSome<T>(this IEnumerable<T> collection, int max = -1) {
            IEnumerable<T> result = new List<T>();
            max = (max > 0 && max <= collection.Count()) ? max : rand.Next(1, collection.Count());
            while (result.Count() < max) {
                T added = collection.pickOne();
                if (!result.Contains(added)) {
                    ((List<T>)result).Add(added);
                }
            }
            return result;
        }
        /// <summary>Randomly picks a number of items from a generic array.</summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="max">Maximum number of items to pick. By default -1, negative values result in a random number of items being picked</param>
        /// <returns>Result generic array of picked items</returns>
        public static Array pickSome<T>(this Array array, int max = -1) {
            List<T> result = new List<T>();
            max = (max > 0 && max <= array.Length) ? max : rand.Next(1, array.Length);
            while (result.Count < max) {
                T added = array.pickOne<T>();
                if (!result.Contains(added)) {
                    result.Add(added);
                }
            }
            Array resultArray = new T[result.Count()];
            for (int i = 0; i < resultArray.Length; i++) {
                resultArray.SetValue(result.ElementAt(i), i);
            }
            return resultArray;
        }
        /// <summary>Randomly picks a number of items from an array.</summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="max">Maximum number of items to pick. By default -1, negative values result in a random number of items being picked</param>
        /// <returns>Result array of picked items</returns>
        public static T[] pickSome<T>(this T[] array, int max = -1) {
            return array.ToList().pickSome(max).ToArray();
        }
        /// <summary>Randomly picks a number of characters from a string.</summary>
        /// <param name="max">Maximum length of the result string. By default -1, negative length will result in a random number of characters chosen</param>
        /// <returns>A string representation of the chosen characters</returns>
        public static string pickSome(this string text, int max = -1) {
            char[] chars = text.ToCharArray().pickSome(max);
            string result = "";
            foreach (char i in chars) result += i;
            return result;
        }

        /// <summary>Attempts to combine a Dictionary of KeyValuePairs into a string</summary>
        /// <param name="space">Delimiting character, by default will be '~'</param>
        /// <returns>A string result of the concatination</returns>
        public static string combine<P, Q>(this IDictionary<P, Q> dictionary, char space = '~') {
            string result = "";
            foreach (KeyValuePair<P, Q> i in dictionary) {
                if (result != "") result += space;
                result += "{ " + i.Key.ToString() + " , " + i.Value.ToString() + " }";
            }
            return result;
        }
        /// <summary>Attempts to combine a Enumerable collection of objects into a string</summary>
        /// <param name="space">Delimiting character, by default will be '~'</param>
        /// <returns>A string result of the concatination</returns>
        public static string combine<T>(this IEnumerable<T> collection, char space = '~') {
            return _combine(collection, space);
        }
        /// <summary>Attempts to combine generic array of objects into a string</summary>
        /// <param name="space">Delimiting character, by default will be '~'</param>
        /// <returns>A string result of the concatination</returns>
        public static string combine<T>(this Array array, char space = '~') {
            return _combine(array, space);
        }
        private static string _combine(object items, char space) {
            string result = "";
            foreach (object i in (IEnumerable<object>)items) {
                if (result != "") result += space;
                result += i.ToString();
            }
            return result;
        }

        /// <summary>Returns a copy of this Dictionary omitting the first element</summary>
        /// <returns>Dictionary af all elements in order except for the first element</returns>
        public static IDictionary<P, Q> Tail<P, Q>(this IDictionary<P, Q> dictionary) {
            List<P> keys = (List<P>)dictionary.Keys.Tail();
            IDictionary<P, Q> result = new Dictionary<P, Q>();
            foreach (P i in keys) result[i] = dictionary[i];
            return result;
        }
        /// <summary>Returns a copy of this collection omitting the first element</summary>
        /// <returns>Collection af all elements in order except for the first element</returns>
        public static IEnumerable<T> Tail<T>(this IEnumerable<T> collection) {
            IEnumerable<T> result = new List<T>(collection);
            ((List<T>)result).RemoveAt(0);
            return result;
        }
        /// <summary>Returns a copy of this array omitting the first element</summary>
        /// <returns>Array af all elements in order except for the first element</returns>
        public static Array Tail(this Array array) {
            Array result = new object[array.Length - 1];
            Array.Copy(array, 1, result, 0, result.Length);
            return result;
        }
        /// <summary>Returns a copy of this array omitting the first element</summary>
        /// <returns>Array af all elements in order except for the first element</returns>
        public static T[] Tail<T>(this T[] array) {
            List<T> result = new List<T>(array);
            result.RemoveAt(0);
            return result.ToArray();
        }

        /// <summary>Removes the first KeyValuePair from the dictionary and returns it as a result</summary>
        /// <returns>First KeyValuePair in the dictionary</returns>
        public static KeyValuePair<P, Q> Head<P, Q>(this IDictionary<P, Q> dictionary) {
            KeyValuePair<P, Q> result = dictionary.ToArray()[0];
            dictionary.Remove(result.Key);
            return result;
        }
        /// <summary>Removes the first element from the collection and returns it as a result</summary>
        /// <returns>First element from the collection</returns>
        public static T Head<T>(this IEnumerable<T> collection) {
            T result = collection.ElementAt(0);
            ((List<T>)collection).RemoveAt(0);
            return result;
        }
        /// <summary>Removes the first element from the array and returns it as a result</summary>
        /// <returns>First element from the array</returns>
        public static T Head<T>(this T[] array) {
            T result = array[0];
            for (int i = 1; i < array.Length; i++) array[i-1] = array[i];
            array[array.Length - 1] = default(T);
            Array.Resize(ref array, array.Length - 1);
            return result;
        }

        public static void Use<T>(this T o, Action<T> actions) { actions(o); }

        public static T AppendTo<T>(this T item, ref T[] array) {
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = item;
            return item;
        }

        public static T InsertAt<T>(this T item, int index, ref T[] array) {
            Array.Resize(ref array, array.Length + 1);
            for (int i = array.Length - 2; i >= index; i--) array[i + 1] = array[i];
            array[index] = item;
            return item;
        }

        public static int RemoveIndexFrom<T>(this int index, ref T[] array) {
            for (int i = index + 1; i < array.Length; i++) {
                array[i - 1] = array[i];
            }
            Array.Resize(ref array, array.Length - 1);
            return index;
        }

        public static T RemoveFrom<T>(this T item, ref T[] array) {
            Array.IndexOf(array, item).RemoveIndexFrom(ref array);
            return item;
        }
    }
}
