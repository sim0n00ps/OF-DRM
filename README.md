# OF-DRM
C# console app to download DRM protected videos from Onlyfans accounts

# Installation
I have tried to make this a lot more simple to set up compared to [OF DL](https://github.com/sim0n00ps/OF-DL). 

The only thing you need to do is go to the [releases](https://github.com/sim0n00ps/OF-DRM/releases) page and download the latest release zip file.

Extract the zip file somewhere safe using 7zip or winrar, you should have 3 files and 2 folders:
- OF DRM.exe
- auth.json
- e_sqlite3.dll
- EXEs - this contains yt-dlp.exe, mp4decrypt.exe and ffmpeg.exe with the paths already set up correctly in the auth.json file. DO NOT TOUCH THEM IF YOU DON'T KNOW WHAT YOU ARE DOING!
- cdm - this is where you need to put your device_client_id_blob and device_private_key files. They need to be placed in `cdm/devices/chrome_1610/`.

Next you need to fill out the auth.json file.
1. Go to www.onlyfans.com and login.
2. Press F12 to open dev tools and select the 'Network' tab.
3. In the search box type 'api'

![image](https://user-images.githubusercontent.com/132307467/235547370-5ef8e273-ebf7-4783-a13a-225f5959c606.png)

4. Click on one of the requests (if nothing shows up refresh the page or click on one of the tabs such as messages to make something appear).
5. After clicking on a request, make sure the headers tab is selected and then scroll down to find the 'Request Headers' section, this is where you should be able to find the information you need.
6. Copy the values of `cookie`, `user-agent`, `user-id` (this should just be a number, do not include a `u`) and `x-bc` to the `auth.json` file where the paths to yt-dlp, ffmpeg and mp4decrypt should already be.
7. Save the file.

You should have something like this:

`"USER_ID": "123456789"` - Do NOT include the `u` that gets exported using the Onlyfans Cookie Helper

`"USER_AGENT": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36"` - Make sure this is set to your user-agent value

`"X_BC": "2a9b28a68e7c03a9f0d3b98c28d70e8105e1f1df"` - Make sure this is set to your x-bc value

`"COOKIE": "auth_id=123456789; sess=k3s9tnzdc8vt2h47ljxpmwqy5r;"` - Make sure you set auth_id to the same value as `user-id` and that you set your `sess` to your actual `sess` value, everytime you log out of Onlyfans this value will change so make sure to update it after every login.

Next you will need to get 2 files, device_client_id_blob and device_private_key. These are used to get the decryption keys needed for downloading DRM videos. You can find a tutorial on how to do this here https://forum.videohelp.com/threads/408031-Dumping-Your-own-L3-CDM-with-Android-Studio

Once you have filled all of the information out you can close auth.json and double click on OF DRM.exe and you should be ready to start downloading videos.

# Videos
This scraper is aimed to download DRM videos only and nothing else.

You will get the choice to:
- Download None of the videos found
- Download All of the videos found
- Select individual videos found, you will be able to see the Post/Message Id along with DateTime of that Post/Message.

To navigate the menu the can use the &#8593; & &#8595; arrows, to select/deselect video(s) press the `space` key and after you are happy with your selection(s) press `enter` to start downloading.

# Config Values
`DownloadPaidPosts`:

If set to `true` then any posts on the users feed that have been purchased by you and have DRM enabled videos will be scraped.

If set to `false` no paid posts will be scraped.

`DownloadPosts`:

If set to `true` then any posts on the users feed that have DRM enabled videos will be scraped.

If set to `false` no posts will be scraped.

`DownloadArchived`:

If set to `true` then any archived posts on the users feed that have DRM enabled videos will be scraped.

If set to `false` no archived posts will be scraped.

`DownloadMessages`:

If set to `true` then any free messages that have DRM enabled videos will be scraped.

If set to `false` no free messages will be scraped.

`DownloadPaidMessages`:

If set to `true` then any paid messages that have been purchased by you and have DRM enabled videos will be scraped.

If set to `false` no paid messages will be scraped.

`IncludeExpiredSubscriptions`:

If set to `true` then any past subscriptions that aren't currently active are included in the list of accounts you can scrape.

If set to `false` then any past subscriptions are excluded from the list of accounts you can scrape from.

`DownloadPath`:

If left blank then content will be downloaded to `__user_data__/sites/OnlyFans/{username}`

An example is if you set the download path to `"S:/"` then content will be downloaded to `S:/{username}`

# Donations
If you would like to donate then here is a link to my ko-fi page https://ko-fi.com/sim0n00ps. Donations are not required but are very much appreciated:)
