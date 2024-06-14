# REVALIDATE

REVALIDATE is a unified way to validate Trackmania replays and ghosts outside of the game.

Since 2003, Trackmania has had an ingame replay validation feature. This feature is not implemented in Trackmania (2020) leaderboards, which sometimes causes record pollution of invalid records that nobody ever cleans. In Trackmania 2, this only applies to Top 10 records of official campaigns, and in Trackmania United Forever, it does not exist at all.

However, all of these games have some form of replay validation available:

- Trackmania (2020) and Trackmania 2 replays can be validated through a dedicated server command. Convenient.
- Trackmania United Forever replays can be validated only with a GUI client executable command. Less convenient, but possible with a fake X server.

## How it works

REVALIDATE is a Hangfire service that handles 4 stages:
1. Gathers replay, ghost, and map files from different Trackmania games via client uploads
2. Schedules validation tests on dedicated servers (and TMUF client executable) running as Docker containers
3. Retrieves outputted validation text files, parses them, links the results with the individual replay/ghost files
4. Send the results to the appropriate users
