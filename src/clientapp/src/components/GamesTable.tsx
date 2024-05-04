import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { solid } from '@fortawesome/fontawesome-svg-core/import.macro';
import { GamesTableProps } from "../interfaces/ISportsPageTypes";
import { Dvr as IDvr } from '../interfaces/IGame';

const getDvrIcon = (dvr: IDvr | undefined) => {
    if (!dvr)
        return null;
    if (dvr.completed)
        return <FontAwesomeIcon className="ml-3" icon={solid("video")} title="Completed" color='#50c77c' />;
    if (dvr.started)
        return <FontAwesomeIcon className="ml-3" icon={solid("video")} title="Recording" color='#c75050' />;

    return <FontAwesomeIcon className="ml-3" icon={solid("video")} title="Scheduled" />;
};

const GamesTable: React.FC<GamesTableProps> = ({ games, onGameSelect }) => (
    <div className="table-wrapper">
        <table className="table is-striped fixed-header-table">
            <thead>
                <tr>
                    <th>Away</th>
                    <th>Home</th>
                    <th>Time</th>
                </tr>
            </thead>
            <tbody>
                {games?.map((game, key) => (
                    <tr>
                        <td>
                            {game.state === 'In Progress' || game.state === 'Halftime' ?
                                <a href='' onClick={(e) => onGameSelect(e, game)}>{game.awayTeam}</a> :
                                game.awayTeam
                            }
                        </td>
                        <td>
                            {game.state === 'In Progress' || game.state === 'Halftime' ?
                                <a href='' onClick={(e) => onGameSelect(e, game)}>{game.homeTeam}</a> :
                                game.homeTeam
                            }
                        </td>
                        <td>
                            {new Date(game.gameTime).toTimeString().split(' ')[0].substring(0, 5)}
                            {getDvrIcon(game.dvr)}
                        </td>
                    </tr>
                ))}
            </tbody>
        </table>
    </div>
);

export default GamesTable;
