## Pandora Plugin for Centrafuse##

### Centrafuse Plugin ###

[Centrafuse Auto](http://www.centrafuse.com/CentrafuseAuto.aspx) is a "complete automotive infotainment software suite" designed to be used an embedded [car PC](http://www.justin-credible.net/Projects/Car-PC).

This is a plugin for Centrafuse that allows the user to play internet radio from the [Pandora Internet Radio](http://www.pandora.com/) service. It allows the user to add, remove, mark as favorite, and play their stations. It also allows a passenger to login as a guest.

More information and screen shots can be found [here](http://www.justin-credible.net/Projects/Centrafuse-Pandora-Plugin).

Centrafuse is written in .NET, and therefore, this plugin is also written in .NET. It utilizes the PandoraSharp library to communicate with the Pandora service.

### PandoraSharp API###

This is the library that was written to communicate with the Pandora service using the XML-RPC API. It was created by examining the source code from [xbmc-pandora](http://gitorious.org/xbmc-pandora) and [libpiano](https://github.com/PromyLOPh/pianobar/tree/master/src/libpiano) as well as using an HTTP debugging proxy (eg [Fiddler](http://fiddler2.com/)) to examine the API calls.

The Pandora API requires the use of an encryption/decryption key that Pandora regularly changes. The key can be obtained by decompiling the SWF player from their website or checking with one of aforementioned projects.

### Pandora API Alternatives ###

If you are looking for a more up-to-date Pandora API for .NET, I highly recommend checking out the engine that is used in the [Pandora Music Box](http://code.google.com/p/pandora-musicbox/) project, which is a plugin for the [MediaPortal](http://www.team-mediaportal.com/) media center application.

### Copyright ###

Copyright © 2014 Justin Unterreiner. See [LICENSE.txt](https://github.com/Justin-Credible/Centrafuse-Pandora-Plugin/blob/master/License.txt) for details.