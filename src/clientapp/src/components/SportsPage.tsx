import React, { useEffect, useState, useRef } from 'react';
import { Game } from '../interfaces/IGame';
import useFetchGames from '../hooks/useFetchGames';
import { SportPageProps } from '../interfaces/ISportsPageTypes';
import TopControls from './TopControls';
import GamesTable from './GamesTable';
import { isMobile } from 'react-device-detect';

const SportPage: React.FC<SportPageProps> = ({ league, advanced }) => {
    const { games, refresh } = useFetchGames(league);
    const playerRef = useRef<any>(null);
    const [showPlayer, setShowPlayer] = useState<boolean>(false);
    const stuckCount = useRef(0);
    const lastStuckTime = useRef(0);

    // New effect to handle "f" key
    useEffect(() => {
        const handleKeydown = (e: KeyboardEvent) => {
            if (e.key === "f") {
                if (playerRef.current) {
                    playerRef.current.core.toggleFullscreen();
                }
            }
        };
        window.addEventListener("keydown", handleKeydown);
        return () => {
            window.removeEventListener("keydown", handleKeydown);
        };
    }, []);

    useEffect(() => {
        // Cleanup when component unmounts
        return () => {
            destroyPlayer();
        };
    }, []);

    const destroyPlayer = () => {
        if (playerRef.current) {
            try {
                playerRef.current.destroy();
            } catch (error) {
                console.error(error);
            }
            playerRef.current = null;
        }

        setShowPlayer(false);
    };

    const onGameStuck = (data: any, game: Game) => {
        if (!(data.details === "levelLoadTimeOut" || data.details === "levelLoadError" || data.details === "fragLoadError" || data.details === "manifestLoadTimeOut" || data.details === "manifestLoadError"))
            return;

        destroyPlayer();

        const now = Date.now();
        console.log('game stuck at ' + new Date().toTimeString() + ' for ' + data.details + ' on ' + game.awayTeam + ' vs ' + game.homeTeam + ' (' + stuckCount.current + ')');

        if (now - lastStuckTime.current > 10000) {
            stuckCount.current = 1;
        } else {
            stuckCount.current++;
        }

        lastStuckTime.current = now;

        if (stuckCount.current < 5) {
            console.log('kickstart due to stream stuck');
            setTimeout(() => {
                onGameSelect(null, game);
            }, 1000);
        } else {
            console.log('Giving up');
            stuckCount.current = 0;
        }
    };

    const onGameSelect = async (event: any, game: Game) => {
        if (event) {
            event.preventDefault();
        }

        destroyPlayer();

        let team = game.awayTeam;

        if (team.indexOf('Sox') !== -1)
            team = game.homeTeam;

        setShowPlayer(true)

        setTimeout(() => {
            // @ts-ignore
            playerRef.current = new Clappr.Player({
                source: `simple/${league}/${team}`,
                // @ts-ignore
                plugins: [HlsjsPlayback],
                mimeType: "application/x-mpegurl",
                parentId: "#player",
                width: '100%',
                height: 'auto',
                autoPlay: true,
                hlsPlayback: {
                    preload: true,
                    customListeners: [
                        { eventName: 'hlsError', callback: (event: any, data: any) => { onGameStuck(data, game) }, once: false }
                    ]
                },
                playback: {
                    hlsjsConfig: {
                        levelLoadingMaxRetry: 5,
                        maxBufferLength: 60
                    }
                }
            });

            // @ts-ignore
            playerRef.current.on(Clappr.Events.PLAYER_PLAY, function () {
                setTimeout(playerRef.current.core.mediaControl.hide(), 2000);
            })

            setTimeout(() => {
                document.getElementById('player')?.scrollIntoView()
            })
        })
    }

    return (
        <section className="section">
            <div className="columns">
                <div className="column">
                    <TopControls refresh={refresh} advanced={advanced} />
                    <GamesTable games={games} onGameSelect={onGameSelect} />
                </div>
                <div className='column'>
                    {showPlayer && (
                        <div>
                            <div id="player"></div>
                            {!isMobile &&
                                <span className="tag is-info is-medium">
                                    <b>F</b>&nbsp; = &nbsp;toggle fullscreen
                                </span>
                            }
                        </div>
                    )}
                </div>
            </div>
        </section>
    );
};

export default SportPage;