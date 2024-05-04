import { useState, useEffect, useMemo } from 'react';
import { Game, Dvr as IDvr } from '../interfaces/IGame';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { solid } from '@fortawesome/fontawesome-svg-core/import.macro'
import { isMobile } from 'react-device-detect';

const Dvr = () => {
    const [games, setGames] = useState<Game[]>([]);
    const [search, setSearch] = useState('');

    const fetchGames = async (searchTerm = '') => {
        try {
            const response = await fetch(`game/all?search=${searchTerm}`);
            const games = await response.json();
            setGames(games);
        } catch (error) {
            console.log(error);
        }
    };

    const getGameTime = (date: Date) => {
        if (window.innerWidth <= 768) { // mobile view
            return `${date.getMonth() + 1}/${date.getDate()} ${date.getHours()}:${String(date.getMinutes()).padStart(2, '0')}`;
        } else { // desktop view
            return date.toLocaleString('en-US', {
                month: '2-digit',
                day: '2-digit',
                year: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            }).replace(',', '');
        }
    }

    const getDvrIcon = (dvr: IDvr | undefined) => {
        if (!dvr)
            return null;
        if (dvr.completed)
            return <FontAwesomeIcon className="ml-3" icon={solid("video")} title="Completed" color='#50c77c' />;
        if (dvr.started)
            return <FontAwesomeIcon className="ml-3" icon={solid("video")} title="Recording" color='#c75050' />;

        return <FontAwesomeIcon className="ml-3" icon={solid("video")}  title="Scheduled" />;
    };

    const toggleRecording = async (game: Game) => {
        if (game.dvr) {

            if (game.dvr.started)
                return;

            await fetch(`dvrmanagement/cancel/${game.id}`, {
                method: 'DELETE',
            })
                .then(x => fetchGames(search))
        }
        else {
            await fetch(`dvrmanagement/schedule/${game.id}`, {
                method: 'POST',
            })
                .then(x => fetchGames(search))
        }
    }

    useEffect(() => {
        fetchGames();
    }, []);

    useEffect(() => {
        const timer = setTimeout(() => fetchGames(search), 300);

        return () => clearTimeout(timer);
    }, [search]);

    const renderedGames = useMemo(() => games.map((game, key) => (
        <tr key={key} onClick={() => toggleRecording(game)} className="clickable-row">
            <td>
                {getGameTime(new Date(game.gameTime))}
                {getDvrIcon(game.dvr)}
            </td>
            <td className="is-hidden-mobile">{game.league}</td>
            <td>{isMobile ? game.shortAwayTeam.charAt(0).toUpperCase() + game.shortAwayTeam.slice(1) : game.awayTeam}</td>
            <td>{isMobile ? game.shortHomeTeam.charAt(0).toUpperCase() + game.shortHomeTeam.slice(1) : game.homeTeam}</td>
        </tr>
    )), [games]);

    return (
        <section className="section">
            <div className="columns">
                <div className="column">
                    <div className="topControls">
                        <div className="field">
                            <div className="control has-icons-left">
                                <input
                                    className="input"
                                    type="search"
                                    placeholder="Search by team..."
                                    value={search}
                                    onChange={(e) => setSearch(e.target.value)}
                                />
                                <span className="icon is-left">
                                    <FontAwesomeIcon icon={solid("search")} />
                                </span>
                            </div>
                        </div>
                    </div>
                    {games && games.length > 0 &&
                        <table className="table is-striped table is-hoverable">
                            <thead>
                                <tr>
                                    <th>Time</th>
                                    <th className="is-hidden-mobile">League</th>
                                    <th>Away</th>
                                    <th>Home</th>
                                </tr>
                            </thead>
                            <tbody>
                                {renderedGames}
                            </tbody>
                        </table>
                    }
                </div>
            </div>
        </section>
    );
};

export default Dvr;
