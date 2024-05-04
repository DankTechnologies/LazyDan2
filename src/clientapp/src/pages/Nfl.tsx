import SportPage from '../components/SportsPage';

const Nfl = ({ advanced }: { advanced: boolean }) => {
    return <SportPage league="nfl" advanced={advanced} />;
}

export default Nfl;