using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Text;

public class ByteHelper
{
	public static byte[] Combine(byte[] first, byte[] second)
	{
		byte[] ret = new byte[first.Length + second.Length];
		Buffer.BlockCopy(first, 0, ret, 0, first.Length);
		Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
		return ret;
	}

	public static byte[] Combine(byte[] first, byte[] second, byte[] third)
	{
		byte[] ret = new byte[first.Length + second.Length + third.Length];
		Buffer.BlockCopy(first, 0, ret, 0, first.Length);
		Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
		Buffer.BlockCopy(third, 0, ret, first.Length + second.Length,
			third.Length);
		return ret;
	}

	public static byte[] Combine(params byte[][] arrays)
	{
		byte[] ret = new byte[arrays.Sum(x => x.Length)];
		int offset = 0;
		foreach (byte[] data in arrays)
		{
			Buffer.BlockCopy(data, 0, ret, offset, data.Length);
			offset += data.Length;
		}
		return ret;
	}

	public static byte[] RemoveBefore(byte[] source, int index){
		int length = source.Length - index;
		byte[] final = new byte[length];
		for (int i = 0; i < length; i++) {
			final [i] = source [i + index];
		}
		return final;
	}

	//Vector3
	public static byte[] vector3Bytes(Vector3 value){
		byte[] xbytes = System.BitConverter.GetBytes (value.x);
		byte[] ybytes = System.BitConverter.GetBytes (value.y);
		byte[] zbytes = System.BitConverter.GetBytes (value.z);
		byte[][] combinedBytes = new byte[][] {xbytes, ybytes, zbytes};
		return Combine (combinedBytes);
	}
	public static Vector3 getVector3(byte[] data, int startIndex){
		float x = System.BitConverter.ToSingle (data, startIndex);
		float y = System.BitConverter.ToSingle (data, startIndex + 4);
		float z = System.BitConverter.ToSingle (data, startIndex + 8);
		return new Vector3 (x, y, z);
	}

	//Quaternion
	public static byte[] quaternionBytes(Quaternion value){
		byte[] xbytes = System.BitConverter.GetBytes (value.x);
		byte[] ybytes = System.BitConverter.GetBytes (value.y);
		byte[] zbytes = System.BitConverter.GetBytes (value.z);
		byte[] wbytes = System.BitConverter.GetBytes (value.w);
		byte[][] combinedBytes = new byte[][] {xbytes, ybytes, zbytes, wbytes};
		return Combine (combinedBytes);
	}
	public static Quaternion getQuaternion(byte[] data, int startIndex){
		float x = System.BitConverter.ToSingle (data, startIndex);
		float y = System.BitConverter.ToSingle (data, startIndex + 4);
		float z = System.BitConverter.ToSingle (data, startIndex + 8);
		float w = System.BitConverter.ToSingle (data, startIndex + 12);
		return new Quaternion (x, y, z, w);
	}

	//Bool
	public static byte boolToByte(bool[] source){
		byte result = 0;
		// This assumes the array never contains more than 8 elements!
		int index = 8 - source.Length;

		// Loop through the array
		foreach (bool b in source){
			// if the element is 'true' set the bit at that position
			if (b)
				result |= (byte)(1 << (7 - index));

			index++;
		}

		return result;
	}
	public static bool[] byteToBool(byte b)
	{
		// prepare the return result
		bool[] result = new bool[8];

		// check each bit in the byte. if 1 set to true, if 0 set to false
		for (int i = 0; i < 8; i++)
			result[i] = (b & (1 << i)) == 0 ? false : true;

		// reverse the array
		Array.Reverse(result);

		return result;
	}

	//String
	public static byte[] stringBytes(string text){
		return ByteHelper.Combine(
			BitConverter.GetBytes((ushort)text.Length),
			System.Text.Encoding.ASCII.GetBytes (text)
		);
	}

	public static byte[] intArrayBytes(int[] intArray){
		byte[] bytes = BitConverter.GetBytes((ushort)intArray.Length);
		for (int i = 0; i < intArray.Length; i++) {
			bytes = ByteHelper.Combine (bytes, BitConverter.GetBytes (intArray [i]));
		}
		return bytes;
	}

	//Color
	public static byte[] colorBytes(Color color){
		byte[] rbytes = System.BitConverter.GetBytes (color.r);
		byte[] gbytes = System.BitConverter.GetBytes (color.g);
		byte[] bbytes = System.BitConverter.GetBytes (color.b);
		byte[] abytes = System.BitConverter.GetBytes (color.a);
		byte[][] combinedBytes = new byte[][] {rbytes, gbytes, bbytes, abytes};
		return Combine (combinedBytes);
	}
	public static Color getColor(byte[] data, int startIndex){
		float r = System.BitConverter.ToSingle (data, startIndex);
		float g = System.BitConverter.ToSingle (data, startIndex + 4);
		float b = System.BitConverter.ToSingle (data, startIndex + 8);
		float a = System.BitConverter.ToSingle (data, startIndex + 12);
		return new Color (r, g, b, a);
	}



	// to convert from anything to byte               	System.BitConverter. GetBytes (value);
	// to convert from byte to anything					System.BitConverter. Something();
	// decoding from bytes to text 						System.Text.Encoding.ASCII.GetString(byte[]);
	// encoding from text to byte						System.Text.Encoding.ASCII.GetBytes(text);

}

public class ByteContructor{
	public List<byte> bytesList = new List<byte> ();

	public ByteContructor(){}
	public ByteContructor(byte[] bytes){
		bytesList.AddRange (bytes);
	}
	public void add(byte[] staff){
		bytesList.AddRange (staff);
	}
	public void addLevels(int[] levels){
		for (int i = 0; i < levels.Length; i++) {
			add(BitConverter.GetBytes(levels[i]));
		}
	}
	public byte[] addLevels(int[] levels, byte[] toSend){
		addLevels (levels);
		add(toSend);
		return bytesList.ToArray ();
	}
}

public class ByteReceiver{
	public byte[] data;
	public int index = 0;
	public ByteReceiver(byte[] dataL){
		data = dataL;
	}
	public ByteReceiver(byte[] dataL, int start){
		data = dataL;
		index = start;
	}
	public byte getByte(){
		var value = data[index];
		index++;
		return value;
	}
	public short getShort(){
		var value = BitConverter.ToInt16 (data, index);
		index += 2;
		return value;
	}
	public int getInt(){
		var value = BitConverter.ToInt32(data,index);
		index+=4;
		return value;
	}
	public float getFloat(){
		var value= BitConverter.ToSingle(data,index);
		index+=4;
		return value;
	}
	public double getDouble(){
		var value= BitConverter.ToDouble(data,index);
		index+=8;
		return value;
	}
	public Vector3 getVector3(){
		var vector3 = ByteHelper.getVector3 (data, index);
		index += 12;
		return vector3;
	}
	public Quaternion getQuaternion(){
		var value = ByteHelper.getQuaternion (data, index);
		index += 16;
		return value;
	}
	public string getString(){
		int length = (ushort)getShort ();
		string toReturn = System.Text.Encoding.ASCII.GetString(data, index, length);
		index += length;
		return toReturn;
	}
	public int[] getIntArray(){
		int length = (ushort)getShort ();
		int[] array = new int[length];
		for (int i = 0; i < length; i++) {
			array[i] = getInt ();
		}
		return array;
	}
//	public string getStringLeft(){
//		int left = data.Length - index;
//		string toReturn = System.Text.Encoding.ASCII.GetString(data, index, left);
//		index = data.Length-1;
//		return toReturn;
//	}
	public Color getColor(){
		Color color = ByteHelper.getColor (data, index);
		index += 16;
		return color;
	}
	public void reset(){
		index = 0;
	}
}
//public class ByteSender : List<byte>{
//	
//	public void AddRange()
//}