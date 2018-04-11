# SubSync
Automatically download subtitles for your movies

I hope this little tool comes to help you out as much as it did for me. My girlfriend and I both struggled with using the VLC Sub add-on for downloading subtitles, a tool that we previously been using lots! And when that tool stopped working on later days, keep being unresponsive and continuesly crashing VLC, we had to manually look for those darn subtitles on the web and if you have a lot of shows to watch then its a huge pain in the ass.

SubSync is a tool that will keep your movies folder synchronized with subtitles. That means, if you add a video file to the folder or its sub-folders being watched a subtitle will automatically be downloaded for that movie.

It works by using the filename of the movie to determine the name and use that to do a search on subscene.com to find the "first best" subtitle to download. It will then extract the file (if its an archive) and rename it to have the same name as the movie file so it can be quickly recognized as a subtitle using VLC. 

SubSync will also prefer a chosen language over the other, so for me I prefer Swedish but if that one does not exist I want English, you can set this as a startup argument.

**Note:**
This tool was created with just a couple of hours so be aware that it may not always work perfectly.
I do appreciate any problems you may stumble upon either code-wise or functionality when/or if testing this application out. Don't be afraid to add an issue!

## Binaries
You can download the somewhat latest binaries from the release tab thingy
https://github.com/zerratar/SubSync/releases

Mirror mirror on the wall
http://www.shinobytes.com/files/SubSync-binaries-win32.zip

## Building SubSync
Load up Visual Studio 2017, open SubSync.sln and hit CTRL+SHIFT+B like your life depends on it!

## Running SubSync
```batch
SubSync.exe <input folder> [<languages>] [<video extensions>]

<input folder>: So this is the folder you want to watch, it will also watch all its subfolders.
                As an example: 'D:\Movies'

<languages>: A list of languages separated by a semi-colon. Example: swedish;english
             The priority of the languages is from left to right, so in this example if 
             a swedish translation subtitle is available it will take that one rather 
             than the english one.

             The value is just English per default.

<video extensions>: A list of video extensions to watch seperated by a semi-colon, just add
                    all you can think of. But this is an optional and default value is:
                    *.avi;*.mp4;*.mkv;*.mpeg;*.flv;*.webm
```

In most cases you will probably only need to run it like this:

```batch
SubSync.exe "D:\My Awesome Movies\"
```

Or if you want to have your subtitles in another language (if one exists) and you're
crazy enough to think you will want Latin before English.

```batch
SubSync.exe "D:\My Awesome Movies\" spanish;japanese;latin;english
```

Now keep it running in the background. Its not going to hog up your cpu. Its pretty friendly, and you can be sure to have subtitles available for you whenever you need it!

## Tips and tricks
Press 'q' at any time to exit SubSync

Press 'a' to try and re-sync any previously unsynced subtitles. Yes the subtitle downloads can randomly fail some times when subscene.com decides you shouldn't be downloading their subtitles too often.

Oh, and be sure to bring popcorns or your favorite snacks when watching your movies!

## Known issues / To-dos

The OpenSubtitles provider ignore language priority.

Add support for http://www.yifysubtitles.com/

## Changes
### v0.1.4
Add support for OpenSubtitles.org and is also now the default subtitle provider for SubSync. subscene.com will still be used but only if no subtitles were found on opensubtitles.org.
Improved subtitle search algorithm, but only for OpenSubtitles.org right now. The subscene provider will be updated in a future version.

The opensubtitles.org provider has not been properly tested though, so there may still be some bugs that needs to be squished. But initial tests returned great results!

### v0.1.3
Use Unicode encoding in the console to properly display all texts.

### v0.1.2
Update the usage of all FileInfo and DirectoryInfo instances to use the ZetaLongPaths available from here https://github.com/UweKeim/ZetaLongPaths to fix the bug caused by too long paths.

### v0.1.1
This version didn't like anyone.

### v0.1.0
Initial release on GitHub, you know the magical first version