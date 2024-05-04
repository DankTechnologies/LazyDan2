import SportPage from '../components/SportsPage';

const Cfb = ({ advanced }: { advanced: boolean }) => {
    return <SportPage league="cfb" advanced={advanced} />;
}

export default Cfb;