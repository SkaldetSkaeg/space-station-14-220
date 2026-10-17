# © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

changecvar-simple-game_played_map_memory_cycle = Exclude played maps until the current map cycle is complete.
changecvar-full-game_played_map_memory_cycle = When played map memory is enabled, remember all played maps regardless of memory depth. Clear the history when no unplayed maps meet the current player count and other selection requirements. Exclude the last played map from the next vote if another map is eligible. Disabling this option restores the configured memory depth.

changecvar-simple-vote_max_maps_per_vote = Maximum number of maps offered in a map vote. Zero disables the limit.
changecvar-full-vote_max_maps_per_vote = Offer at most this many randomly selected eligible maps in each map vote. Set to zero to offer all eligible maps. If fewer maps are eligible than the limit, offer all of them.
