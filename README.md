## What is Pack?

Pack lets you take any file (say an excel workbook) and encode it as a PNG image for sharing online where file uploads aren't usually allowed.

## Why?

It's not possible to share files like test data etc in GitHub issues / pull requests, by avoiding e-mail / shared drives, Pack overcomes:

1. Forgetting where that data is
2. Accidental deletion of that data
3. Requirement to store the file on a shared drive / in an e-mail inbox

## Limitations

You can store pretty much any type of file you want to, Pack will ensure a minimum size of 200x200 pixels to make the image more visible; it fills in the non-data parts of the image with nice colours.

The data itself is LZMA compressed (7-zip) to help reduce the size of the image, but obviously some files are just going to be too big to practically share this way.

### What about multiple files?

You can share multiple files in a single image, just compress them first (e.g. a zip / 7-zip archive).

## How do I use it?

Just launch the application and you'll see the main screen:

![App Window](/readme-assets/appwin.PNG) 

From here just drag the file you want to share, or the Pack'd image from a website; the app will then create an image for you / offer to save the unpacked file.

Once an image has been created, you'll see something like:

![New Image](/readme-assets/imagecreate.PNG) 

Now you just need to drag that image into wherever you want to share it, this will typically be the GitHub comment box.

## How does it work?

At a very high-level it does the following:

1. Compresses the file you give it
2. Drops the trailing zeroes from the end of the compressed data and remembers how many zeroes it got rid of (this makes the image smaller)
3. Creates a byte array containing the following:
    1. A special token so the app can recognise it's a Pack'd image
    2. The number of bytes the filename is long
    3. The filename in UTF-8 byte form
    4. The number of zeroes that were dropped
    5. The length of the data being written
    6. The compressed data
4. Groups the data into chunks of 4 bytes
    * This is because the image is 32-bit ARGB meaning 4 bytes can be encoded in a single pixel
5. Works out how big the image needs to be to accommodate all the data
6. Adjust the image size up if it's less than the minimum size
7. Walks bottom > top and left > right in the image
8. Sets the colour and alpha of each pixel based on data meant to go at that point
    1. If no data is available for this point, it uses a random colour from a pallette
    2. It also adds a black border around the non-data section on the left, right and bottom edges
9. Saves the image somewhere and makes it draggable from the app.

To get back from an image is basically the same in reverse, except:

1. **All** the data from the image is read (including the random section)
2. The token, filename, dropped and length data is read out
3. The data is checked (e.g. is token correct)
4. The specific section of the data that is the compressed data is read
5. The compressed data is padded with the dropped zeroes
6. The compressed data is uncompressed
7. The app saves the uncompressed data where you choose.

