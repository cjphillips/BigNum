# BigNum
## Description
The BigNum class represents an immutable object that stores and performs operations on base-ten numbers. Like the [BigInteger](https://msdn.microsoft.com/en-us/library/system.numerics.biginteger(v=vs.110).aspx) structure found in .NET's System.Numerics namespace, BigNum performs these operations with lossless precision (with the exception of division; see below). 

The purpose of the BigNum object is to provide a way to represent extremely large numbers and perform operations on them with lossless precision. Floats and doubles are restricted to base-two logic as well as a limited range of numbers overall. As a result, [precision is not guaranteed](http://docs.oracle.com/cd/E19957-01/806-3568/ncg_goldberg.html) is many scenarios. For example, 2.4 in base-ten cannot be accurately represented in binary, and is thus estimated to be around 2.399999999.... repeating. This is an issue when accuracy is crucial or when large numbers are often used (think banking / accounting). 

BigNum allows numbers as large as memory can hold. There is no limit on the size of the number, but exception handling is always advised. 
## Installation
TODO: Describe the installation process
## Usage
A BigNum object can be instantiated in a couple of different ways:

* BigNum(string)
	*  Takes a string representation of a (base-ten) decimal number and create a BigNum from it.
	* An implicit operator is also available (e.g. BigNum num = "12.052").
* BigNum(double, bool)
	* Takes a double value and does one two operations:
		* If the boolean value is true, the double's .ToString() method will be called and the BigNum's first constructor (see above) will be used. This method does **not** guarantee precision as double.ToString() itself is not precise. 
		* Otherwise, the BigNum will be created by parsing the double's [IEEE 754 bit pattern](https://en.wikipedia.org/wiki/Double-precision_floating-point_format). Again, this method does **not** guarantee precision as the double itself is not precise.
	* Note: The implicit assignment operation (e.g. BigNum num = 12.052) uses this second constructor with false as the second argument.

## Contributing
1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request!

## Credits
Thus far, all work done on BigNum has been my own work. Any contributions are welcomed!

## License
TODO
