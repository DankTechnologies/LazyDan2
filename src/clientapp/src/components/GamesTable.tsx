import { GamesTableProps } from "../interfaces/ISportsPageTypes";
import DvrIcon from './DvrIcon';

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
                            {game.state === 'In Progress' || game.state === 'Halftime' || game.state === 'End of Period' ?
                                <a href='' onClick={(e) => onGameSelect(e, game)}>{game.awayTeam}</a> :
                                game.awayTeam
                            }
                        </td>
                        <td>
                            {game.state === 'In Progress' || game.state === 'Halftime' || game.state === 'End of Period' ?
                                <a href='' onClick={(e) => onGameSelect(e, game)}>{game.homeTeam}</a> :
                                game.homeTeam
                            }
                        </td>
                        <td>
                            {new Date(game.gameTime).toTimeString().split(' ')[0].substring(0, 5)}
                            <DvrIcon game={game} />
                        </td>
                    </tr>
                ))}
            </tbody>
        </table>
    </div>
);

export default GamesTable;
