import SportPage from '../components/SportsPage';

const Mlb = ({ advanced }: { advanced: boolean }) => {
    return <SportPage league="mlb" advanced={advanced} />;
}

export default Mlb;