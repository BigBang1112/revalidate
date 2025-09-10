# Revalidate

Revalidate is a unified solution to validate Trackmania replays and ghosts remotely (aka: outside of the game).

Since 2003, Trackmania has had an ingame replay validation feature, but since Trackmania released in 2020, it has largely degrated - users cannot validate their replays ingame anymore. But it was found that some form of validation is still available.

Leaderboards maintained by Nadeo are mostly not automatically validated:
- In Trackmania (2020), leaderboards have an internal flagging system that probably uses replay validation but is still moderated manually (even less on community campaigns).
- In Trackmania 2, replay validation applies only to Top 10 records and is evaluated in the morning. Invalid records are automatically removed.
- In Trackmania United Forever, leaderboards don't use replay validation at all.

However, all of these games have some form of I/O replay validation available:

- Trackmania (2020) and Trackmania 2 replays can be validated through a dedicated server command. Convenient.
- Trackmania United Forever replays can be validated only with a GUI client executable command. Less convenient, but possible with a fake X server.

So, instead of focusing on a single Trackmania game, Revalidate handles TM2020, TM2, and TMUF with no difference of user experience.

## How it works

Revalidate is a web API service that handles 4 stages:
1. Gathers replay, ghost, and map files from different Trackmania games via client uploads
2. Schedules validation tests on dedicated servers (and TMUF client executable) running as Docker containers
3. Retrieves standard output and validation text files, parses them, links the results with the individual replay/ghost files
4. Stores the validation results in a database (if not opted-out)
5. Sends the validation results back to users

Ghosts can be validated alone on maps that are known and stored on Revalidate. For replays, you can opt in for validation against a different map than the one stored in the replay file.

### Flow

For validation request:

1. **Input**: files as multipart form
2. Check for 0 files - if true, validation request failed 
3. Check for >10 files (default) - if true, validation request failed
4. Create a validation request model planned to be stored in the database
5. For each file:
    1. Check for empty file - if true, append validation warning and skip the file
    2. Check for file above 8MB (default) - if true, append validation warning and skip the file
    3. Parse file as Gbx - expected Replay.Gbx, Ghost.Gbx, or Map.Gbx without GBX.NET errors, otherwise append validation warning and skip the file
    4. Generate SHA256 of the file and discard duplicates - append validation warning and skip the file
    5. Store/memorize all validation info and useful map info + unmodified binary of the file
        - For Replay.Gbx: if multiple are present, then pick the first one (multi-ghost replays are likely not validable), always store to database
        - For Ghost.Gbx: store temporarily (wait until all maps are gathered)
        - For Map.Gbx: store temporarily (wait until all replays and ghosts are gathered)
6. After all files are gathered, for each *valid* Gbx:
    1. Finalize the validation request:
        - If Replay.Gbx: check if the map with matching `Validate_ChallengeUid` is provided by Revalidate or from imported Map.Gbx:
            - If yes: extract Ghost.Gbx into its own binary file create a separate validation (can be prone to GBX.NET errors, so it's additional)
            - If not: ghost extraction is skipped
        - If Ghost.Gbx: check if the map with matching `Validate_ChallengeUid` is provided by Revalidate or from imported Map.Gbx:
            - If yes: store the ghost to database (info + unmodified binary)
            - If not: append validation warning (missing map to validate against)
        - If Map.Gbx: check if there's already a ghost or a replay that matches the map's `MapUid`
            - If yes: store useful map details for quick access + full unmodified binary of the file to the database
            - If not: append validation warning (map is not useful for validation)
7. If out of all uploads, there is *not* a single ghost with a map alongside it (also meant as Replay.Gbx), validation request failed (by using validation warnings as details)
8. Save the transaction of the validation request to the database
9. Notify validation job about the request that was considered usable for validation using a channel writer
10. **Output**: Validation result (progress)

For validation job (running in the background):

1. Check for incomplete validation requests from database, rerun them first
2. Receive new validation requests in a loop using channel readers
3. For each validation request:
    1. Copy unmodified replay/ghost/map binaries to their appropriate UserData volumes
        - For TM2/020:
            - Replay.Gbx and Ghost.Gbx to Replays/.
            - Map.Gbx to Maps/.
        - For TMUF:
            - Replay.Gbx and Ghost.Gbx to Tracks/Replays/.
            - Challenge.Gbx to Tracks/.
    2. Execute replay validation:
        - For TM2/020:
            1. Run ManiaServerManager container with `MSM_VALIDATE_PATH=.` and read stdout/stderr separately in real time
            2. For each new JSON object, write the result in a subchannel and store it in a database
        - For TMUF:
            1. Run TMForeverNoGUI container with `/validatepath=.` (TMForeverNoGUI project is not publically available)
    3. After container exits, store the ValidationLog.txt file in a database
    4. Save the transaction of the validation result to the database
        - Validation result (progress) becomes Validation result (completed)
    5. Cleanup ManiaServerManager/TMForeverNoGUI

Validation events are also reflected on the `/validations/{id}/events` endpoint via server-sent events using channel readers.

## Web API

### Endpoints

#### Validation endpoints

Submit the request with `POST /validate[/against]` and retrieve the results with `GET /validate[/against]`.

The `POST` request also supports a long-polling option that holds the request until the validation completes, but this can take a very long time when the app instance is intensively used. Prefer using it in development or with a large enough timeout.

Validation endpoints either support long-polling (make sure you have a large enough timeout), or you can view the results by using a `GET` request.

- `POST /validate` - Validate a Replay.Gbx, using the map file inside the replay.
- `POST /validate/against` - Validate a Replay.Gbx against a different map than the one stored inside the replay, or a Ghost.Gbx against a specific Map.Gbx. If a map file is not provided, the replay is validated against the map stored on the server.
- `GET /validate[/against]` - View results of the `POST /validate[/against]` endpoint. The results will be automatically deleted 1-2 hours after completion (in scheduled batches).
- `DELETE /validate[/against]` - Delete the validation results early.

## CLI

Planned in the future.
