import { useEffect, useState, useCallback } from "react";
import { Game } from "../interfaces/IGame";

const useFetchGames = (league: string) => {
  const [games, setGames] = useState<Game[]>();

  const fetchGames = useCallback(async () => {
    try {
      const response = await fetch(`game/${league}`);
      const games = await response.json();
      setGames(games);
    } catch (error) {
      console.log(error);
    }
  }, [league]);

  useEffect(() => {
    fetchGames();
  }, [fetchGames]);

  return { games, refresh: fetchGames };
};

export default useFetchGames;
