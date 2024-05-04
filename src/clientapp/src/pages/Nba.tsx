import SportPage from '../components/SportsPage';

const Nba = ({ advanced }: { advanced: boolean }) => {
    return <SportPage league="nba" advanced={advanced} />;
}

export default Nba;