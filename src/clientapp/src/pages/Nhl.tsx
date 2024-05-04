import SportPage from '../components/SportsPage';

const Nhl = ({ advanced }: { advanced: boolean }) => {
    return <SportPage league="nhl" advanced={advanced} />;
}

export default Nhl;