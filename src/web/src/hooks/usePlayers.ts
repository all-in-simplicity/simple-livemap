import { useSSE } from 'react-hooks-sse';
import { Player } from '../types/player.ts';

type State = {
    players: Array<Player> | null;
};

type Message = Array<Player> | [];

export const usePlayers = () => {
    const state = useSSE<State, Message>(
        'positions',
        {
            players: [],
        },
        {
            stateReducer(_, action) {
                return {
                    players: action.data,
                };
            },
            parser(input: string) {
                return JSON.parse(input) as Message;
            },
        }
    );
    return { players: state.players };
};
