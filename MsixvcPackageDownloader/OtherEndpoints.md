
These are all of the endpoints I found relating to MSIXVC link generation:

- https://packagespc.xboxlive.com/GetBasePackage/<ContentId> | Get all available information about a given package (Latest base package, .phf files, multiple .xsp files)
- https://packagespc.xboxlive.com/GetSpecificBasePackage/<ContentId>/<UpdateId> | Get all available information about a given package and its update id. (Note: Does not return information about previous updates, at least from my testing.)
- https://updatepc.xboxlive.com/IsContentUpdateAvailable | POST: `{"contentId": "<ContentId>", "versionId": "<VersionId>", "updateMode": <UpdateMode enum int>}` | Returns information about available updates and their packages.
