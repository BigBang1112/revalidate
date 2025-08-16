# REVALIDATE

REVALIDATE is a unified way to validate Trackmania replays and ghosts outside of the game.

Since 2003, Trackmania has had an ingame replay validation feature. This feature was not properly implemented in Trackmania (2020) leaderboards until 2024, which sometimes causes record pollution of invalid records that nobody ever cleans. In Trackmania 2, this only applies to Top 10 records of official campaigns, and in Trackmania United Forever, it does not exist at all.

However, all of these games have some form of replay validation available:

- Trackmania (2020) and Trackmania 2 replays can be validated through a dedicated server command. Convenient.
- Trackmania United Forever replays can be validated only with a GUI client executable command. Less convenient, but possible with a fake X server.

## How it works

REVALIDATE is a Hangfire service that handles 4 stages:
1. Gathers replay, ghost, and map files from different Trackmania games via client uploads
2. Schedules validation tests on dedicated servers (and TMUF client executable) running as Docker containers
3. Retrieves outputted validation text files, parses them, links the results with the individual replay/ghost files
4. Send the results to the appropriate users

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
